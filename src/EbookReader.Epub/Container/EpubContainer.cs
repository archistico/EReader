using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace EbookReader.Epub.Container;

/// <summary>
/// Read-only EPUB Open Container Format (OCF) archive.
/// </summary>
public sealed class EpubContainer : IDisposable
{
    public const string EpubMimeType = "application/epub+zip";
    public const string ContainerXmlPath = "META-INF/container.xml";

    private static readonly XNamespace ContainerNamespace =
        "urn:oasis:names:tc:opendocument:xmlns:container";

    private readonly Stream _sourceStream;
    private readonly ZipArchive _archive;
    private readonly bool _leaveOpen;
    private readonly ReadOnlyDictionary<string, ZipArchiveEntry> _entries;
    private bool _disposed;

    private EpubContainer(
        Stream sourceStream,
        ZipArchive archive,
        bool leaveOpen,
        Dictionary<string, ZipArchiveEntry> entries,
        ReadOnlyCollection<EpubRootFile> rootFiles)
    {
        _sourceStream = sourceStream;
        _archive = archive;
        _leaveOpen = leaveOpen;
        _entries = new ReadOnlyDictionary<string, ZipArchiveEntry>(entries);
        RootFiles = rootFiles;
        EntryPaths = Array.AsReadOnly(entries.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    public IReadOnlyList<EpubRootFile> RootFiles { get; }

    public EpubRootFile DefaultRootFile => RootFiles[0];

    public IReadOnlyList<string> EntryPaths { get; }

    public static EpubContainer Open(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.SequentialScan);
        bool ownershipTransferred = false;

        try
        {
            EpubContainer container = Open(stream, leaveOpen: false);
            ownershipTransferred = true;
            return container;
        }
        finally
        {
            if (!ownershipTransferred)
            {
                stream.Dispose();
            }
        }
    }

    public static EpubContainer Open(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("Lo stream EPUB deve essere leggibile e seekable.", nameof(stream));
        }

        ValidateFirstLocalHeader(stream);

        ZipArchive archive;
        try
        {
            stream.Position = 0;
            archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: Encoding.UTF8);
        }
        catch (InvalidDataException exception)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InvalidZip,
                "Il file non è un archivio ZIP EPUB leggibile.",
                exception);
        }
        catch (NotSupportedException exception)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.UnsupportedZipFeature,
                "Il contenitore EPUB usa una funzione ZIP non supportata.",
                exception);
        }

        bool opened = false;
        try
        {
            try
            {
                Dictionary<string, ZipArchiveEntry> entries = BuildEntryIndex(archive);
                ValidateMimeTypeEntry(entries);
                ReadOnlyCollection<EpubRootFile> rootFiles = ReadRootFiles(entries);
                ValidateRootFilesExist(entries, rootFiles);
                EpubContainer container = new(stream, archive, leaveOpen, entries, rootFiles);
                opened = true;
                return container;
            }
            catch (InvalidDataException exception)
            {
                throw new EpubContainerException(
                    EpubContainerErrorCode.InvalidZip,
                    "La struttura ZIP del contenitore EPUB è danneggiata o non supportata.",
                    exception);
            }
            catch (NotSupportedException exception)
            {
                throw new EpubContainerException(
                    EpubContainerErrorCode.UnsupportedZipFeature,
                    "La struttura ZIP del contenitore EPUB usa una funzione non supportata.",
                    exception);
            }
        }
        finally
        {
            if (!opened)
            {
                archive.Dispose();
            }
        }
    }

    public bool Contains(OcfPath path)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(path);
        return _entries.ContainsKey(path.Value);
    }

    public Stream OpenEntry(OcfPath path)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(path);

        if (!_entries.TryGetValue(path.Value, out ZipArchiveEntry? entry))
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.EntryNotFound,
                $"La risorsa '{path.Value}' non esiste nel contenitore EPUB.");
        }

        return OpenValidatedEntry(entry, path.Value);
    }

    public Stream OpenDefaultPackageDocument() => OpenEntry(DefaultRootFile.Path);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _archive.Dispose();
        if (!_leaveOpen)
        {
            _sourceStream.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }


    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    private static void ValidateFirstLocalHeader(Stream stream)
    {
        ZipLocalHeader header = ZipLocalHeader.ReadFirst(stream);

        if (!string.Equals(header.FileName, "mimetype", StringComparison.Ordinal))
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.MimeTypeNotFirst,
                "La prima entry fisica del contenitore EPUB deve essere 'mimetype'.");
        }

        if (header.UsesEncryption)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.UnsupportedZipFeature,
                "La cifratura ZIP non è consentita nei contenitori EPUB OCF.");
        }

        if (header.VersionNeeded is not (10 or 20 or 45))
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.UnsupportedZipFeature,
                $"Version needed to extract ZIP non supportata: {header.VersionNeeded}.");
        }

        if (header.CompressionMethod != 0)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.MimeTypeCompressed,
                "L'entry 'mimetype' deve essere memorizzata senza compressione.");
        }

        if (header.ExtraFieldLength != 0)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.MimeTypeHasExtraField,
                "L'entry 'mimetype' non può avere extra field nel local ZIP header.");
        }
    }

    private static Dictionary<string, ZipArchiveEntry> BuildEntryIndex(ZipArchive archive)
    {
        if (archive.Entries.Count > EpubContainerLimits.MaxEntries)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.TooManyEntries,
                $"Il contenitore supera il limite di {EpubContainerLimits.MaxEntries} entry.");
        }

        Dictionary<string, ZipArchiveEntry> entries = new(StringComparer.Ordinal);
        long totalUncompressedBytes = 0;

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.Name.Length == 0)
            {
                continue;
            }

            OcfPath path = OcfPath.FromArchiveEntry(entry.FullName);
            ValidateArchiveEntrySecurity(entry, path);

            if (entry.Length > EpubContainerLimits.MaxTotalUncompressedBytes - totalUncompressedBytes)
            {
                throw new EpubContainerException(
                    EpubContainerErrorCode.ArchiveUncompressedSizeTooLarge,
                    $"Il contenitore supera il limite cumulativo di {EpubContainerLimits.MaxTotalUncompressedBytes} byte decompressi dichiarati.");
            }

            totalUncompressedBytes += entry.Length;
            if (!entries.TryAdd(path.Value, entry))
            {
                throw new EpubContainerException(
                    EpubContainerErrorCode.DuplicateContainerEntry,
                    $"Entry OCF duplicata: '{path.Value}'.");
            }
        }

        return entries;
    }

    private static void ValidateArchiveEntrySecurity(ZipArchiveEntry entry, OcfPath path)
    {
        int unixMode = (entry.ExternalAttributes >> 16) & 0xFFFF;
        const int unixFileTypeMask = 0xF000;
        const int unixRegularFile = 0x8000;
        int unixFileType = unixMode & unixFileTypeMask;
        if (unixFileType is not (0 or unixRegularFile))
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.UnsafeArchiveEntryType,
                $"La entry ZIP '{path.Value}' dichiara un tipo file Unix speciale non ammesso in EReader.");
        }

        if (entry.Length < 0 || entry.CompressedLength < 0)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InconsistentArchiveEntry,
                $"La entry ZIP '{path.Value}' dichiara dimensioni non valide.");
        }

        if (entry.Length > EpubContainerLimits.MaxEntryUncompressedBytes)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.ArchiveEntryTooLarge,
                $"La entry ZIP '{path.Value}' supera il limite di {EpubContainerLimits.MaxEntryUncompressedBytes} byte decompressi.");
        }

        if (entry.Length < EpubContainerLimits.CompressionRatioInspectionThresholdBytes)
        {
            return;
        }

        if (entry.CompressedLength == 0 ||
            (double)entry.Length / entry.CompressedLength > EpubContainerLimits.MaxCompressionRatio)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.SuspiciousCompressionRatio,
                $"La entry ZIP '{path.Value}' dichiara un rapporto di compressione patologico oltre {EpubContainerLimits.MaxCompressionRatio}:1.");
        }
    }

    private static ValidatedZipEntryStream OpenValidatedEntry(ZipArchiveEntry entry, string path)
    {
        try
        {
            return new ValidatedZipEntryStream(entry.Open(), path, entry.Length);
        }
        catch (InvalidDataException exception)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InconsistentArchiveEntry,
                $"La entry ZIP '{path}' è corrotta o incoerente con la directory centrale.",
                exception);
        }
        catch (NotSupportedException exception)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.UnsupportedZipFeature,
                $"La entry ZIP '{path}' usa un metodo di compressione o una funzione ZIP non supportata.",
                exception);
        }
    }

    private static void ValidateMimeTypeEntry(Dictionary<string, ZipArchiveEntry> entries)
    {
        if (!entries.TryGetValue("mimetype", out ZipArchiveEntry? mimetypeEntry))
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.MissingMimeType,
                "Il contenitore EPUB non contiene l'entry 'mimetype'.");
        }

        if (mimetypeEntry.Length > EpubContainerLimits.MaxMimeTypeBytes)
        {
            throw InvalidMimeType();
        }

        using ValidatedZipEntryStream stream = OpenValidatedEntry(mimetypeEntry, "mimetype");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        byte[] actual = buffer.ToArray();
        byte[] expected = Encoding.ASCII.GetBytes(EpubMimeType);

        if (!actual.AsSpan().SequenceEqual(expected))
        {
            throw InvalidMimeType();
        }
    }

    private static ReadOnlyCollection<EpubRootFile> ReadRootFiles(
        Dictionary<string, ZipArchiveEntry> entries)
    {
        if (!entries.TryGetValue(ContainerXmlPath, out ZipArchiveEntry? containerEntry))
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.MissingContainerXml,
                $"Il contenitore EPUB non contiene '{ContainerXmlPath}'.");
        }

        if (containerEntry.Length > EpubContainerLimits.MaxContainerXmlBytes)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.ContainerXmlTooLarge,
                $"'{ContainerXmlPath}' supera il limite di {EpubContainerLimits.MaxContainerXmlBytes} byte.");
        }

        XDocument document = LoadContainerXml(containerEntry);
        XElement? container = document.Root;
        if (container?.Name != ContainerNamespace + "container")
        {
            throw InvalidContainerXml("Root element o namespace di container.xml non valido.");
        }

        if (!string.Equals((string?)container.Attribute("version"), "1.0", StringComparison.Ordinal))
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InvalidContainerVersion,
                "container.xml deve dichiarare version='1.0'.");
        }

        XElement[] rootfilesElements = container
            .Elements(ContainerNamespace + "rootfiles")
            .ToArray();

        if (rootfilesElements.Length != 1)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.MissingRootfiles,
                "container.xml deve contenere esattamente un elemento rootfiles.");
        }

        XElement[] rootfileElements = rootfilesElements[0]
            .Elements(ContainerNamespace + "rootfile")
            .ToArray();

        if (rootfileElements.Length == 0)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.MissingRootfiles,
                "container.xml deve contenere almeno un elemento rootfile.");
        }

        if (rootfileElements.Length > EpubContainerLimits.MaxRootfiles)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.TooManyRootfiles,
                $"container.xml supera il limite di {EpubContainerLimits.MaxRootfiles} rootfile.");
        }

        List<EpubRootFile> rootFiles = new(rootfileElements.Length);
        foreach (XElement rootfileElement in rootfileElements)
        {
            string? fullPath = (string?)rootfileElement.Attribute("full-path");
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new EpubContainerException(
                    EpubContainerErrorCode.InvalidRootfile,
                    "Un elemento rootfile non contiene l'attributo full-path.");
            }

            string? mediaType = (string?)rootfileElement.Attribute("media-type");
            if (!string.Equals(
                    mediaType,
                    EpubRootFile.PackageDocumentMediaType,
                    StringComparison.Ordinal))
            {
                throw new EpubContainerException(
                    EpubContainerErrorCode.InvalidRootfileMediaType,
                    $"Media type rootfile non valido per '{fullPath}'.");
            }

            rootFiles.Add(new EpubRootFile(
                OcfPath.FromContainerReference(fullPath),
                EpubRootFile.PackageDocumentMediaType));
        }

        return rootFiles.AsReadOnly();
    }

    private static XDocument LoadContainerXml(ZipArchiveEntry containerEntry)
    {
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = EpubContainerLimits.MaxContainerXmlBytes,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
        };

        try
        {
            using ValidatedZipEntryStream stream = OpenValidatedEntry(containerEntry, ContainerXmlPath);
            using XmlReader reader = XmlReader.Create(stream, settings);
            return XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InvalidContainerXml,
                "META-INF/container.xml non è XML valido o usa costrutti XML non consentiti.",
                exception);
        }
    }

    private static void ValidateRootFilesExist(
        Dictionary<string, ZipArchiveEntry> entries,
        ReadOnlyCollection<EpubRootFile> rootFiles)
    {
        foreach (EpubRootFile rootFile in rootFiles)
        {
            if (!entries.ContainsKey(rootFile.Path.Value))
            {
                throw new EpubContainerException(
                    EpubContainerErrorCode.RootfileNotFound,
                    $"Il package document '{rootFile.Path.Value}' dichiarato da container.xml non esiste.");
            }
        }
    }

    private static EpubContainerException InvalidMimeType() =>
        new(
            EpubContainerErrorCode.InvalidMimeTypeContent,
            $"L'entry 'mimetype' deve contenere esattamente '{EpubMimeType}' in US-ASCII.");

    private static EpubContainerException InvalidContainerXml(string message) =>
        new(EpubContainerErrorCode.InvalidContainerXml, message);
}
