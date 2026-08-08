using System.IO.Compression;
using System.Text;
using EbookReader.Epub.Container;

namespace EbookReader.Epub.Tests.Container;

public sealed class EpubContainerTests
{
    [Fact]
    public void ValidContainerOpensAndSelectsDefaultRootfile()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid();
        using EpubContainer container = EpubContainer.Open(input, leaveOpen: true);

        Assert.Equal("EPUB/package.opf", container.DefaultRootFile.Path.Value);
        Assert.Equal(EpubRootFile.PackageDocumentMediaType, container.DefaultRootFile.MediaType);
        Assert.Contains(EpubContainer.ContainerXmlPath, container.EntryPaths);
        Assert.True(input.CanRead);
    }

    [Fact]
    public void FirstRootfileIsDefaultWhenMultipleAreDeclared()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
              <rootfiles>
                <rootfile full-path="EPUB/first.opf" media-type="application/oebps-package+xml" />
                <rootfile full-path="EPUB/second.opf" media-type="application/oebps-package+xml" />
              </rootfiles>
            </container>
            """;

        using MemoryStream input = EpubFixtureFactory.CreateValid(
            containerXml: xml,
            packageEntryPath: "EPUB/first.opf",
            additionalEntries: [("EPUB/second.opf", "<package />")]);
        using EpubContainer container = EpubContainer.Open(input);

        Assert.Equal(2, container.RootFiles.Count);
        Assert.Equal("EPUB/first.opf", container.DefaultRootFile.Path.Value);
    }

    [Fact]
    public void PercentEncodedRootfileResolvesToZipFileName()
    {
        string xml = EpubFixtureFactory.CreateContainerXml("EPUB/My%20Book.opf");
        using MemoryStream input = EpubFixtureFactory.CreateValid(
            containerXml: xml,
            packageEntryPath: "EPUB/My Book.opf");
        using EpubContainer container = EpubContainer.Open(input);

        Assert.Equal("EPUB/My Book.opf", container.DefaultRootFile.Path.Value);
    }

    [Fact]
    public void OpenDefaultPackageDocumentReturnsDeclaredEntry()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid();
        using EpubContainer container = EpubContainer.Open(input);
        using Stream package = container.OpenDefaultPackageDocument();
        using StreamReader reader = new(package, Encoding.UTF8);

        Assert.Equal("<package />", reader.ReadToEnd());
    }

    [Fact]
    public void MissingArbitraryEntryHasDedicatedErrorCode()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid();
        using EpubContainer container = EpubContainer.Open(input);
        OcfPath missing = OcfPath.FromArchiveEntry("EPUB/missing.xhtml");

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => container.OpenEntry(missing));

        Assert.Equal(EpubContainerErrorCode.EntryNotFound, exception.ErrorCode);
    }

    [Fact]
    public void DisposeClosesOwnedStream()
    {
        MemoryStream input = EpubFixtureFactory.CreateValid();
        EpubContainer container = EpubContainer.Open(input, leaveOpen: false);

        container.Dispose();

        Assert.False(input.CanRead);
    }

    [Fact]
    public void DisposePreservesLeaveOpenStream()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid();
        EpubContainer container = EpubContainer.Open(input, leaveOpen: true);

        container.Dispose();

        Assert.True(input.CanRead);
    }

    [Fact]
    public void OperationsAfterDisposeAreRejected()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid();
        EpubContainer container = EpubContainer.Open(input, leaveOpen: true);
        OcfPath path = OcfPath.FromArchiveEntry("EPUB/package.opf");
        container.Dispose();

        Assert.Throws<ObjectDisposedException>(() => container.Contains(path));
        Assert.Throws<ObjectDisposedException>(() => container.OpenEntry(path));
    }

    [Fact]
    public void RootfileLookupIsCaseSensitive()
    {
        string xml = EpubFixtureFactory.CreateContainerXml("epub/package.opf");
        using MemoryStream input = EpubFixtureFactory.CreateValid(containerXml: xml);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.RootfileNotFound, exception.ErrorCode);
    }

    [Fact]
    public void MissingContainerXmlIsRejected()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid(includeContainerXml: false);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.MissingContainerXml, exception.ErrorCode);
    }

    [Fact]
    public void MissingDeclaredRootfileIsRejected()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid(includePackageEntry: false);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.RootfileNotFound, exception.ErrorCode);
    }

    [Fact]
    public void WrongContainerVersionIsRejected()
    {
        string xml = EpubFixtureFactory.CreateContainerXml("EPUB/package.opf", version: "2.0");
        using MemoryStream input = EpubFixtureFactory.CreateValid(containerXml: xml);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerVersion, exception.ErrorCode);
    }

    [Fact]
    public void WrongRootfileMediaTypeIsRejected()
    {
        string xml = EpubFixtureFactory.CreateContainerXml("EPUB/package.opf", mediaType: "text/xml");
        using MemoryStream input = EpubFixtureFactory.CreateValid(containerXml: xml);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.InvalidRootfileMediaType, exception.ErrorCode);
    }

    [Fact]
    public void ContainerWithoutRootfilesIsRejected()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
              <rootfiles />
            </container>
            """;
        using MemoryStream input = EpubFixtureFactory.CreateValid(containerXml: xml);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.MissingRootfiles, exception.ErrorCode);
    }

    [Fact]
    public void WrongContainerNamespaceIsRejected()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <container version="1.0">
              <rootfiles>
                <rootfile full-path="EPUB/package.opf" media-type="application/oebps-package+xml" />
              </rootfiles>
            </container>
            """;
        using MemoryStream input = EpubFixtureFactory.CreateValid(containerXml: xml);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerXml, exception.ErrorCode);
    }

    [Fact]
    public void MalformedContainerXmlIsRejected()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid(containerXml: "<container>");

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerXml, exception.ErrorCode);
    }

    [Fact]
    public void DtdInContainerXmlIsRejected()
    {
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE container [<!ENTITY x "package.opf">]>
            <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
              <rootfiles>
                <rootfile full-path="EPUB/&x;" media-type="application/oebps-package+xml" />
              </rootfiles>
            </container>
            """;
        using MemoryStream input = EpubFixtureFactory.CreateValid(containerXml: xml);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerXml, exception.ErrorCode);
    }

    [Fact]
    public void RootfileTraversalAboveContainerRootIsRejected()
    {
        string xml = EpubFixtureFactory.CreateContainerXml("../package.opf");
        using MemoryStream input = EpubFixtureFactory.CreateValid(containerXml: xml);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void RootfileInternalDotSegmentsAreNormalized()
    {
        string xml = EpubFixtureFactory.CreateContainerXml("EPUB/Temp/../package.opf");
        using MemoryStream input = EpubFixtureFactory.CreateValid(containerXml: xml);
        using EpubContainer container = EpubContainer.Open(input);

        Assert.Equal("EPUB/package.opf", container.DefaultRootFile.Path.Value);
    }

    [Fact]
    public void InvalidZipEntryTraversalIsRejectedBeforeUse()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid(
            additionalEntries: [("EPUB/../escape.txt", "bad")]);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.InvalidContainerPath, exception.ErrorCode);
    }

    [Fact]
    public void DuplicateZipEntryIsRejected()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid(
            additionalEntries: [("EPUB/package.opf", "duplicate")]);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.DuplicateContainerEntry, exception.ErrorCode);
    }

    [Fact]
    public void MimeTypeMustBeFirstPhysicalEntry()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid(mimeTypeFirst: false);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.MimeTypeNotFirst, exception.ErrorCode);
    }

    [Fact]
    public void MimeTypeContentMustBeExact()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid(
            mimeTypeContent: EpubContainer.EpubMimeType + "\n");

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.InvalidMimeTypeContent, exception.ErrorCode);
    }

    [Fact]
    public void CompressedMimeTypeIsRejected()
    {
        using MemoryStream input = EpubFixtureFactory.CreateValid(compressMimeType: true);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.MimeTypeCompressed, exception.ErrorCode);
    }

    [Fact]
    public void SymbolicLinkArchiveEntryIsRejected()
    {
        using MemoryStream input = new();
        using (ZipArchive archive = new(input, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: Encoding.UTF8))
        {
            EpubFixtureFactory.AddTextEntry(archive, "mimetype", EpubContainer.EpubMimeType, CompressionLevel.NoCompression);
            EpubFixtureFactory.AddTextEntry(
                archive,
                EpubContainer.ContainerXmlPath,
                EpubFixtureFactory.CreateContainerXml("EPUB/package.opf"),
                CompressionLevel.Optimal);
            EpubFixtureFactory.AddTextEntry(archive, "EPUB/package.opf", "<package />", CompressionLevel.Optimal);
            ZipArchiveEntry link = archive.CreateEntry("EPUB/unsafe-link", CompressionLevel.NoCompression);
            link.ExternalAttributes = unchecked((int)0xA1FF0000);
            using Stream output = link.Open();
            output.WriteByte(0x78);
        }

        input.Position = 0;
        EpubContainerException exception = Assert.Throws<EpubContainerException>(() => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.UnsafeArchiveEntryType, exception.ErrorCode);
    }

    [Fact]
    public void PathologicalCompressionRatioIsRejectedBeforeEntryUse()
    {
        byte[] payload = new byte[17 * 1024 * 1024];
        using MemoryStream input = EpubFixtureFactory.CreateValid(
            additionalBinaryEntries: [("EPUB/pathological.bin", payload)]);

        EpubContainerException exception = Assert.Throws<EpubContainerException>(() => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.SuspiciousCompressionRatio, exception.ErrorCode);
    }

    [Fact]
    public void NonZipInputIsRejected()
    {
        using MemoryStream input = new(Encoding.UTF8.GetBytes("not a zip"));

        EpubContainerException exception = Assert.Throws<EpubContainerException>(
            () => EpubContainer.Open(input));

        Assert.Equal(EpubContainerErrorCode.InvalidZip, exception.ErrorCode);
    }

    [Fact]
    public void NonSeekableInputIsRejected()
    {
        using MemoryStream backing = EpubFixtureFactory.CreateValid();
        using NonSeekableReadStream input = new(backing);

        Assert.Throws<ArgumentException>(() => EpubContainer.Open(input));
    }

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly Stream _inner;

        public NonSeekableReadStream(Stream inner)
        {
            _inner = inner;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => _inner.Read(buffer);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
