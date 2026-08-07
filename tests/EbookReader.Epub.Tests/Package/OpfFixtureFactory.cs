using EbookReader.Epub.Container;
using EbookReader.Epub.Tests.Container;

namespace EbookReader.Epub.Tests.Package;

internal static class OpfFixtureFactory
{
    public const string PackagePath = "EPUB/package.opf";

    public static MemoryStream CreateEpub3(
        string? metadata = null,
        string? manifest = null,
        string? spine = null,
        string packageAttributes = "",
        (string Path, string Content)[]? additionalEntries = null)
    {
        string package = CreateEpub3Package(metadata, manifest, spine, packageAttributes);
        (string Path, string Content)[] resources = additionalEntries ?? DefaultResources();
        return EpubFixtureFactory.CreateValid(
            packageEntryPath: PackagePath,
            additionalEntries: resources,
            packageContent: package);
    }

    public static MemoryStream CreateEpub2(
        string? metadata = null,
        string? manifest = null,
        string? spine = null,
        (string Path, string Content)[]? additionalEntries = null)
    {
        string package = $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <package xmlns="http://www.idpf.org/2007/opf" version="2.0" unique-identifier="book-id">
          <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
            {metadata ?? DefaultMetadata(includeModified: false)}
          </metadata>
          <manifest>
            {manifest ?? "<item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" />"}
          </manifest>
          <spine toc="ncx">
            {spine ?? "<itemref idref=\"c1\" />"}
          </spine>
        </package>
        """;

        (string Path, string Content)[] resources = additionalEntries ??
        [
            ("EPUB/toc.ncx", "<ncx />"),
            ("EPUB/Text/ch1.xhtml", "<html />"),
        ];

        return EpubFixtureFactory.CreateValid(
            packageEntryPath: PackagePath,
            additionalEntries: resources,
            packageContent: package);
    }

    public static string CreateEpub3Package(
        string? metadata = null,
        string? manifest = null,
        string? spine = null,
        string packageAttributes = "") =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="book-id" {packageAttributes}>
          <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
            {metadata ?? DefaultMetadata(includeModified: true)}
          </metadata>
          <manifest>
            {manifest ?? DefaultManifest()}
          </manifest>
          <spine>
            {spine ?? DefaultSpine()}
          </spine>
        </package>
        """;

    public static string DefaultMetadata(bool includeModified) =>
        $"""
        <dc:identifier id="book-id">urn:uuid:12345678-1234-1234-1234-123456789abc</dc:identifier>
        <dc:title xml:lang="it">  Titolo   di prova  </dc:title>
        <dc:language>it</dc:language>
        <dc:creator id="creator-1">Emilie Rollandin</dc:creator>
        {(includeModified ? "<meta property=\"dcterms:modified\">2026-08-07T14:00:00Z</meta>" : string.Empty)}
        """;

    public static string DefaultManifest() =>
        """
        <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav" />
        <item id="c1" href="Text/ch1.xhtml" media-type="application/xhtml+xml" />
        <item id="c2" href="Text/ch2.xhtml" media-type="application/xhtml+xml" />
        <item id="cover" href="images/cover.jpg" media-type="image/jpeg" properties="cover-image" />
        """;

    public static string DefaultSpine() =>
        """
        <itemref idref="c1" />
        <itemref idref="c2" linear="no" />
        """;

    public static (string Path, string Content)[] DefaultResources() =>
    [
        ("EPUB/nav.xhtml", "<html />"),
        ("EPUB/Text/ch1.xhtml", "<html />"),
        ("EPUB/Text/ch2.xhtml", "<html />"),
        ("EPUB/images/cover.jpg", "not-a-real-jpeg"),
    ];
}
