using EbookReader.Epub.Container;
using EbookReader.Epub.Package;
using EbookReader.Epub.Tests.Container;

namespace EbookReader.Epub.Tests.Package;

public sealed class EpubPackageReaderTests
{
    [Fact]
    public void ReadsEpub3MetadataManifestAndSpine()
    {
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3();
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageDocument package = EpubPackageReader.Read(container);

        Assert.Equal("3.0", package.Version);
        Assert.True(package.IsEpub3);
        Assert.False(package.IsEpub2);
        Assert.Equal("book-id", package.UniqueIdentifierId);
        Assert.Equal("urn:uuid:12345678-1234-1234-1234-123456789abc", package.Metadata.UniqueIdentifier);
        Assert.Equal("Titolo di prova", Assert.Single(package.Metadata.Titles));
        Assert.Equal("it", Assert.Single(package.Metadata.Languages));
        Assert.Equal("Emilie Rollandin", Assert.Single(package.Metadata.Creators));
        Assert.Equal("2026-08-07T14:00:00Z", package.Metadata.Modified);
        Assert.Equal(4, package.Manifest.Count);
        Assert.Equal(2, package.Spine.Count);
    }

    [Fact]
    public void ResolvesManifestHrefRelativeToPackageDocument()
    {
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3();
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageDocument package = EpubPackageReader.Read(container);

        Assert.Equal("EPUB/Text/ch1.xhtml", package.GetManifestItem("c1").LocalPath?.Value);
        Assert.Equal("EPUB/images/cover.jpg", package.GetManifestItem("cover").LocalPath?.Value);
    }

    [Fact]
    public void DecodesPercentEscapesAndNormalizesDotSegments()
    {
        string manifest = "<item id=\"c1\" href=\"./Text/A%20B.xhtml\" media-type=\"application/xhtml+xml\" />";
        (string Path, string Content)[] entries = [("EPUB/Text/A B.xhtml", "<html />")];
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3(
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />",
            additionalEntries: entries);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageDocument package = EpubPackageReader.Read(container);

        Assert.Equal("EPUB/Text/A B.xhtml", package.GetManifestItem("c1").LocalPath?.Value);
    }

    [Fact]
    public void AcceptsAbsoluteRemoteManifestResource()
    {
        string manifest =
            "<item id=\"c1\" href=\"https://example.com/book/ch1.xhtml\" media-type=\"application/xhtml+xml\" />";
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3(
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />",
            additionalEntries: []);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageDocument package = EpubPackageReader.Read(container);
        EpubManifestItem item = package.GetManifestItem("c1");

        Assert.True(item.IsRemote);
        Assert.Null(item.LocalPath);
        Assert.Equal("https://example.com/book/ch1.xhtml", item.RemoteUri?.AbsoluteUri);
    }

    [Fact]
    public void ReadsManifestProperties()
    {
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3();
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageDocument package = EpubPackageReader.Read(container);

        Assert.True(package.GetManifestItem("nav").HasProperty("nav"));
        Assert.True(package.GetManifestItem("cover").HasProperty("cover-image"));
        Assert.Equal("nav", package.NavigationDocument?.Id);
    }

    [Fact]
    public void TreatsMissingLinearAsYes()
    {
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3();
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageDocument package = EpubPackageReader.Read(container);

        Assert.True(package.Spine[0].IsLinear);
        Assert.False(package.Spine[1].IsLinear);
    }

    [Fact]
    public void ReadsEpub2SpineTocAndLegacyDcAttributes()
    {
        string metadata = """
        <dc:identifier id="book-id" opf:scheme="ISBN" xmlns:opf="http://www.idpf.org/2007/opf">9780000000000</dc:identifier>
        <dc:title>Legacy book</dc:title>
        <dc:language>en</dc:language>
        <dc:creator opf:role="aut" opf:file-as="Rollandin, Emilie" xmlns:opf="http://www.idpf.org/2007/opf">Emilie Rollandin</dc:creator>
        """;
        using MemoryStream stream = OpfFixtureFactory.CreateEpub2(metadata: metadata);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageDocument package = EpubPackageReader.Read(container);

        Assert.True(package.IsEpub2);
        Assert.Equal("ncx", package.SpineTocId);
        EpubDublinCoreMetadata creator = package.Metadata.DublinCore.Single(value => string.Equals(value.Name, "creator", StringComparison.Ordinal));
        Assert.Equal("aut", creator.Role);
        Assert.Equal("Rollandin, Emilie", creator.FileAs);
        EpubDublinCoreMetadata identifier = package.Metadata.DublinCore.Single(value => string.Equals(value.Name, "identifier", StringComparison.Ordinal));
        Assert.Equal("ISBN", identifier.Scheme);
    }

    [Fact]
    public void ReadsPageProgressionDirection()
    {
        string packageText = OpfFixtureFactory.CreateEpub3Package()
            .Replace("<spine>", "<spine page-progression-direction=\"rtl\">", StringComparison.Ordinal);
        using MemoryStream stream = EpubFixtureFactory.CreateValid(
            packageEntryPath: OpfFixtureFactory.PackagePath,
            additionalEntries: OpfFixtureFactory.DefaultResources(),
            packageContent: packageText);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageDocument package = EpubPackageReader.Read(container);

        Assert.Equal("rtl", package.PageProgressionDirection);
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("3.3")]
    [InlineData("4.0")]
    public void RejectsUnsupportedPackageVersion(string version)
    {
        string packageText = OpfFixtureFactory.CreateEpub3Package()
            .Replace("version=\"3.0\"", $"version=\"{version}\"", StringComparison.Ordinal);

        EpubPackageException exception = ParseFailure(packageText);

        Assert.Equal(EpubPackageErrorCode.UnsupportedPackageVersion, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingMetadataElement()
    {
        string packageText = OpfFixtureFactory.CreateEpub3Package();
        int start = packageText.IndexOf("<metadata", StringComparison.Ordinal);
        int end = packageText.IndexOf("</metadata>", StringComparison.Ordinal) + "</metadata>".Length;
        packageText = packageText.Remove(start, end - start);

        EpubPackageException exception = ParseFailure(packageText);

        Assert.Equal(EpubPackageErrorCode.MissingMetadata, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingManifestElement()
    {
        string packageText = OpfFixtureFactory.CreateEpub3Package();
        int start = packageText.IndexOf("<manifest>", StringComparison.Ordinal);
        int end = packageText.IndexOf("</manifest>", StringComparison.Ordinal) + "</manifest>".Length;
        packageText = packageText.Remove(start, end - start);

        EpubPackageException exception = ParseFailure(packageText);

        Assert.Equal(EpubPackageErrorCode.MissingManifest, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingSpineElement()
    {
        string packageText = OpfFixtureFactory.CreateEpub3Package();
        int start = packageText.IndexOf("<spine>", StringComparison.Ordinal);
        int end = packageText.IndexOf("</spine>", StringComparison.Ordinal) + "</spine>".Length;
        packageText = packageText.Remove(start, end - start);

        EpubPackageException exception = ParseFailure(packageText);

        Assert.Equal(EpubPackageErrorCode.MissingSpine, exception.ErrorCode);
    }

    [Fact]
    public void RejectsWrongPackageNamespace()
    {
        string packageText = OpfFixtureFactory.CreateEpub3Package()
            .Replace("http://www.idpf.org/2007/opf", "urn:not-opf", StringComparison.Ordinal);

        EpubPackageException exception = ParseFailure(packageText);

        Assert.Equal(EpubPackageErrorCode.InvalidPackageNamespace, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingUniqueIdentifierAttribute()
    {
        string packageText = OpfFixtureFactory.CreateEpub3Package()
            .Replace(" unique-identifier=\"book-id\"", string.Empty, StringComparison.Ordinal);

        EpubPackageException exception = ParseFailure(packageText);

        Assert.Equal(EpubPackageErrorCode.MissingUniqueIdentifierAttribute, exception.ErrorCode);
    }

    [Fact]
    public void RejectsUniqueIdentifierThatDoesNotResolve()
    {
        string packageText = OpfFixtureFactory.CreateEpub3Package()
            .Replace("unique-identifier=\"book-id\"", "unique-identifier=\"missing\"", StringComparison.Ordinal);

        EpubPackageException exception = ParseFailure(packageText);

        Assert.Equal(EpubPackageErrorCode.UniqueIdentifierNotFound, exception.ErrorCode);
    }

    [Fact]
    public void RejectsEpub3WithoutModifiedMetadata()
    {
        string metadata = OpfFixtureFactory.DefaultMetadata(includeModified: false);
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3(metadata: metadata);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageException exception = Assert.Throws<EpubPackageException>(() => EpubPackageReader.Read(container));

        Assert.Equal(EpubPackageErrorCode.MissingModifiedMetadata, exception.ErrorCode);
    }

    [Fact]
    public void RejectsDuplicatePublicationModifiedMetadata()
    {
        string metadata = OpfFixtureFactory.DefaultMetadata(includeModified: true) +
            "<meta property=\"dcterms:modified\">2026-08-07T15:00:00Z</meta>";
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3(metadata: metadata);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageException exception = Assert.Throws<EpubPackageException>(() => EpubPackageReader.Read(container));

        Assert.Equal(EpubPackageErrorCode.InvalidModifiedMetadata, exception.ErrorCode);
    }

    [Fact]
    public void RejectsInvalidPublicationModifiedTimestamp()
    {
        string metadata = OpfFixtureFactory.DefaultMetadata(includeModified: true)
            .Replace("2026-08-07T14:00:00Z", "2026-08-07 14:00:00+02:00", StringComparison.Ordinal);
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3(metadata: metadata);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageException exception = Assert.Throws<EpubPackageException>(() => EpubPackageReader.Read(container));

        Assert.Equal(EpubPackageErrorCode.InvalidModifiedMetadata, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingTitle()
    {
        string metadata = OpfFixtureFactory.DefaultMetadata(includeModified: true)
            .Replace("<dc:title xml:lang=\"it\">  Titolo   di prova  </dc:title>", string.Empty, StringComparison.Ordinal);
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3(metadata: metadata);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageException exception = Assert.Throws<EpubPackageException>(() => EpubPackageReader.Read(container));

        Assert.Equal(EpubPackageErrorCode.MissingTitle, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingLanguage()
    {
        string metadata = OpfFixtureFactory.DefaultMetadata(includeModified: true)
            .Replace("<dc:language>it</dc:language>", string.Empty, StringComparison.Ordinal);
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3(metadata: metadata);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);

        EpubPackageException exception = Assert.Throws<EpubPackageException>(() => EpubPackageReader.Read(container));

        Assert.Equal(EpubPackageErrorCode.MissingLanguage, exception.ErrorCode);
    }

    [Fact]
    public void RejectsTraversalOutsideContainerRootFromPackageHref()
    {
        string manifest = "<item id=\"c1\" href=\"../../outside.xhtml\" media-type=\"application/xhtml+xml\" />";
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"c1\" />"));

        Assert.Equal(EpubPackageErrorCode.InvalidManifestHref, exception.ErrorCode);
    }

    [Fact]
    public void RejectsPercentEncodedPathSeparatorInPackageHref()
    {
        string manifest = "<item id=\"c1\" href=\"Text%2Fch1.xhtml\" media-type=\"application/xhtml+xml\" />";
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"c1\" />"));

        Assert.Equal(EpubPackageErrorCode.InvalidManifestHref, exception.ErrorCode);
    }

    [Fact]
    public void RejectsDuplicateManifestId()
    {
        string manifest = """
        <item id="c1" href="Text/ch1.xhtml" media-type="application/xhtml+xml" />
        <item id="c1" href="Text/ch2.xhtml" media-type="application/xhtml+xml" />
        """;
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"c1\" />"));

        Assert.Equal(EpubPackageErrorCode.DuplicateManifestId, exception.ErrorCode);
    }

    [Fact]
    public void RejectsDuplicateNormalizedManifestResource()
    {
        string manifest = """
        <item id="a" href="Text/ch1.xhtml" media-type="application/xhtml+xml" />
        <item id="b" href="./Text/ch1.xhtml" media-type="application/xhtml+xml" />
        """;
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"a\" />"));

        Assert.Equal(EpubPackageErrorCode.DuplicateManifestResource, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingLocalManifestResource()
    {
        string manifest = "<item id=\"missing\" href=\"Text/missing.xhtml\" media-type=\"application/xhtml+xml\" />";
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"missing\" />"));

        Assert.Equal(EpubPackageErrorCode.ManifestResourceNotFound, exception.ErrorCode);
    }

    [Fact]
    public void RejectsPackageDocumentSelfReference()
    {
        string manifest = "<item id=\"self\" href=\"package.opf\" media-type=\"application/oebps-package+xml\" />";
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"self\" />"));

        Assert.Equal(EpubPackageErrorCode.PackageDocumentSelfReference, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingFallbackTarget()
    {
        string manifest = "<item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" fallback=\"missing\" />";
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"c1\" />"));

        Assert.Equal(EpubPackageErrorCode.InvalidFallbackReference, exception.ErrorCode);
    }

    [Fact]
    public void RejectsCircularFallbackChain()
    {
        string manifest = """
        <item id="a" href="Text/ch1.xhtml" media-type="application/xhtml+xml" fallback="b" />
        <item id="b" href="Text/ch2.xhtml" media-type="application/xhtml+xml" fallback="a" />
        """;
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"a\" />"));

        Assert.Equal(EpubPackageErrorCode.CircularFallback, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMissingMediaOverlayTarget()
    {
        string manifest = "<item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" media-overlay=\"missing\" />";
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"c1\" />"));

        Assert.Equal(EpubPackageErrorCode.InvalidMediaOverlayReference, exception.ErrorCode);
    }

    [Fact]
    public void RejectsSpineReferenceToMissingManifestItem()
    {
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(spine: "<itemref idref=\"missing\" />"));

        Assert.Equal(EpubPackageErrorCode.SpineManifestItemNotFound, exception.ErrorCode);
    }

    [Fact]
    public void RejectsSpineWithNoLinearItems()
    {
        string spine = "<itemref idref=\"c1\" linear=\"no\" /><itemref idref=\"c2\" linear=\"no\" />";
        EpubPackageException exception = ParseFailure(OpfFixtureFactory.CreateEpub3Package(spine: spine));

        Assert.Equal(EpubPackageErrorCode.NoLinearSpineItem, exception.ErrorCode);
    }

    [Fact]
    public void RejectsInvalidLinearValue()
    {
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(spine: "<itemref idref=\"c1\" linear=\"true\" />"));

        Assert.Equal(EpubPackageErrorCode.InvalidSpineItem, exception.ErrorCode);
    }

    [Fact]
    public void RejectsInvalidPageProgressionDirection()
    {
        string packageText = OpfFixtureFactory.CreateEpub3Package()
            .Replace("<spine>", "<spine page-progression-direction=\"sideways\">", StringComparison.Ordinal);

        EpubPackageException exception = ParseFailure(packageText);

        Assert.Equal(EpubPackageErrorCode.InvalidPageProgressionDirection, exception.ErrorCode);
    }

    [Fact]
    public void RejectsLocalHrefWithFragment()
    {
        string manifest = "<item id=\"c1\" href=\"Text/ch1.xhtml#part\" media-type=\"application/xhtml+xml\" />";
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"c1\" />"));

        Assert.Equal(EpubPackageErrorCode.InvalidManifestHref, exception.ErrorCode);
    }

    [Fact]
    public void RejectsFileUri()
    {
        string manifest = "<item id=\"c1\" href=\"file:///tmp/ch1.xhtml\" media-type=\"application/xhtml+xml\" />";
        EpubPackageException exception = ParseFailure(
            OpfFixtureFactory.CreateEpub3Package(manifest: manifest, spine: "<itemref idref=\"c1\" />"));

        Assert.Equal(EpubPackageErrorCode.InvalidManifestHref, exception.ErrorCode);
    }

    [Fact]
    public void RejectsMalformedPackageXml()
    {
        EpubPackageException exception = ParseFailure("<package");

        Assert.Equal(EpubPackageErrorCode.InvalidPackageXml, exception.ErrorCode);
    }

    [Fact]
    public void RejectsPackageDocumentOverSizeLimit()
    {
        string padding = new('x', (4 * 1024 * 1024) + 1);
        string packageText = OpfFixtureFactory.CreateEpub3Package() + $"<!--{padding}-->";

        EpubPackageException exception = ParseFailure(packageText);

        Assert.Equal(EpubPackageErrorCode.PackageDocumentTooLarge, exception.ErrorCode);
    }

    [Fact]
    public void RejectsDtd()
    {
        string packageText = OpfFixtureFactory.CreateEpub3Package()
            .Replace("<package", "<!DOCTYPE package [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><package", StringComparison.Ordinal);

        EpubPackageException exception = ParseFailure(packageText);

        Assert.Equal(EpubPackageErrorCode.InvalidPackageXml, exception.ErrorCode);
    }

    [Fact]
    public void PublicCollectionsAreReadOnlyViews()
    {
        using MemoryStream stream = OpfFixtureFactory.CreateEpub3();
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        EpubPackageDocument package = EpubPackageReader.Read(container);

        Assert.IsAssignableFrom<IReadOnlyList<EpubManifestItem>>(package.Manifest);
        Assert.IsAssignableFrom<IReadOnlyList<EpubSpineItem>>(package.Spine);
        Assert.IsAssignableFrom<IReadOnlyList<EpubDublinCoreMetadata>>(package.Metadata.DublinCore);
    }

    private static EpubPackageException ParseFailure(string packageText)
    {
        using MemoryStream stream = EpubFixtureFactory.CreateValid(
            packageEntryPath: OpfFixtureFactory.PackagePath,
            additionalEntries: OpfFixtureFactory.DefaultResources(),
            packageContent: packageText);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        return Assert.Throws<EpubPackageException>(() => EpubPackageReader.Read(container));
    }
}
