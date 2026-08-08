using System.IO.Compression;
using System.Text;
using EbookReader.Epub.Container;

namespace EbookReader.Epub.Tests.Container;

internal static class EpubFixtureFactory
{
    public static MemoryStream CreateValid(
        string? containerXml = null,
        string packageEntryPath = "EPUB/package.opf",
        string mimeTypeContent = EpubContainer.EpubMimeType,
        bool mimeTypeFirst = true,
        bool compressMimeType = false,
        IReadOnlyList<(string Path, string Content)>? additionalEntries = null,
        (string Path, byte[] Content)[]? additionalBinaryEntries = null,
        bool includeContainerXml = true,
        bool includePackageEntry = true,
        string? packageContent = null)
    {
        MemoryStream stream = new();

        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: Encoding.UTF8))
        {
            if (!mimeTypeFirst)
            {
                AddTextEntry(archive, "before.txt", "before", CompressionLevel.NoCompression);
            }

            AddTextEntry(
                archive,
                "mimetype",
                mimeTypeContent,
                compressMimeType ? CompressionLevel.Optimal : CompressionLevel.NoCompression);

            if (includeContainerXml)
            {
                AddTextEntry(
                    archive,
                    EpubContainer.ContainerXmlPath,
                    containerXml ?? CreateContainerXml(packageEntryPath),
                    CompressionLevel.Optimal);
            }

            if (includePackageEntry)
            {
                AddTextEntry(archive, packageEntryPath, packageContent ?? "<package />", CompressionLevel.Optimal);
            }

            if (additionalEntries is not null)
            {
                foreach ((string path, string content) in additionalEntries)
                {
                    AddTextEntry(archive, path, content, CompressionLevel.Optimal);
                }
            }

            if (additionalBinaryEntries is not null)
            {
                foreach ((string path, byte[] content) in additionalBinaryEntries)
                {
                    AddBinaryEntry(archive, path, content, CompressionLevel.Optimal);
                }
            }
        }

        stream.Position = 0;
        return stream;
    }

    public static string CreateContainerXml(
        string fullPath,
        string mediaType = EpubRootFile.PackageDocumentMediaType,
        string version = "1.0") =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <container version="{version}" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
          <rootfiles>
            <rootfile full-path="{fullPath}" media-type="{mediaType}" />
          </rootfiles>
        </container>
        """;

    public static void AddTextEntry(
        ZipArchive archive,
        string path,
        string content,
        CompressionLevel compressionLevel)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compressionLevel);
        using Stream output = entry.Open();
        using StreamWriter writer = new(output, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: false);
        writer.Write(content);
    }

    public static void AddBinaryEntry(
        ZipArchive archive,
        string path,
        ReadOnlySpan<byte> content,
        CompressionLevel compressionLevel)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compressionLevel);
        using Stream output = entry.Open();
        output.Write(content);
    }
}
