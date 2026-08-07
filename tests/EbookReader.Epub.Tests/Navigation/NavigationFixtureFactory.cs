using EbookReader.Epub.Container;
using EbookReader.Epub.Navigation;
using EbookReader.Epub.Package;
using EbookReader.Epub.Tests.Container;
using EbookReader.Epub.Tests.Package;

namespace EbookReader.Epub.Tests.Navigation;

internal static class NavigationFixtureFactory
{
    public static EpubNavigationDocument ReadEpub3(
        string navigationXhtml,
        string? manifest = null,
        string? spine = null,
        (string Path, string Content)[]? additionalEntries = null)
    {
        using MemoryStream stream = CreateEpub3(navigationXhtml, manifest, spine, additionalEntries);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        EpubPackageDocument package = EpubPackageReader.Read(container);
        return EpubNavigationReader.Read(container, package);
    }

    public static EpubNavigationException ReadEpub3Failure(
        string navigationXhtml,
        string? manifest = null,
        string? spine = null,
        (string Path, string Content)[]? additionalEntries = null)
    {
        using MemoryStream stream = CreateEpub3(navigationXhtml, manifest, spine, additionalEntries);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        EpubPackageDocument package = EpubPackageReader.Read(container);
        return Assert.Throws<EpubNavigationException>(() => EpubNavigationReader.Read(container, package));
    }

    public static EpubNavigationDocument ReadEpub2(
        string ncx,
        string? manifest = null,
        string? spineAttributes = "toc=\"ncx\"",
        string? spine = null,
        (string Path, string Content)[]? additionalEntries = null)
    {
        using MemoryStream stream = CreateEpub2(ncx, manifest, spineAttributes, spine, additionalEntries);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        EpubPackageDocument package = EpubPackageReader.Read(container);
        return EpubNavigationReader.Read(container, package);
    }

    public static EpubNavigationException ReadEpub2Failure(
        string ncx,
        string? manifest = null,
        string? spineAttributes = "toc=\"ncx\"",
        string? spine = null,
        (string Path, string Content)[]? additionalEntries = null)
    {
        using MemoryStream stream = CreateEpub2(ncx, manifest, spineAttributes, spine, additionalEntries);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        EpubPackageDocument package = EpubPackageReader.Read(container);
        return Assert.Throws<EpubNavigationException>(() => EpubNavigationReader.Read(container, package));
    }

    public static string ValidEpub3Navigation(string tocItems) =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
          <head><title>Navigation</title></head>
          <body>
            <nav epub:type="toc">
              <h1>Contents</h1>
              <ol>{tocItems}</ol>
            </nav>
          </body>
        </html>
        """;

    public static string ValidNcx(string navPoints, bool includeDoctype = false) =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        {(includeDoctype ? "<!DOCTYPE ncx PUBLIC \"-//NISO//DTD ncx 2005-1//EN\" \"http://www.daisy.org/z3986/2005/ncx-2005-1.dtd\">" : string.Empty)}
        <ncx xmlns="http://www.daisy.org/z3986/2005/ncx/" version="2005-1">
          <head><meta name="dtb:uid" content="book-id" /></head>
          <docTitle><text>Test</text></docTitle>
          <navMap>{navPoints}</navMap>
        </ncx>
        """;

    private static MemoryStream CreateEpub3(
        string navigationXhtml,
        string? manifest,
        string? spine,
        (string Path, string Content)[]? additionalEntries)
    {
        (string Path, string Content)[] defaults =
        [
            ("EPUB/nav.xhtml", navigationXhtml),
            ("EPUB/Text/ch1.xhtml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body id=\"body\"><h1 id=\"s1\">One</h1></body></html>"),
            ("EPUB/Text/ch2.xhtml", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><h1 id=\"s2\">Two</h1></body></html>"),
        ];

        (string Path, string Content)[] entries = Merge(defaults, additionalEntries);
        return OpfFixtureFactory.CreateEpub3(
            manifest: manifest ?? "<item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" /><item id=\"c2\" href=\"Text/ch2.xhtml\" media-type=\"application/xhtml+xml\" />",
            spine: spine ?? "<itemref idref=\"c1\" /><itemref idref=\"c2\" />",
            additionalEntries: entries);
    }

    private static MemoryStream CreateEpub2(
        string ncx,
        string? manifest,
        string? spineAttributes,
        string? spine,
        (string Path, string Content)[]? additionalEntries)
    {
        string package = $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <package xmlns="http://www.idpf.org/2007/opf" version="2.0" unique-identifier="book-id">
          <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
            {OpfFixtureFactory.DefaultMetadata(includeModified: false)}
          </metadata>
          <manifest>
            {manifest ?? "<item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" /><item id=\"c2\" href=\"Text/ch2.xhtml\" media-type=\"application/xhtml+xml\" />"}
          </manifest>
          <spine {spineAttributes ?? string.Empty}>
            {spine ?? "<itemref idref=\"c1\" /><itemref idref=\"c2\" />"}
          </spine>
        </package>
        """;

        (string Path, string Content)[] defaults =
        [
            ("EPUB/toc.ncx", ncx),
            ("EPUB/Text/ch1.xhtml", "<html />"),
            ("EPUB/Text/ch2.xhtml", "<html />"),
        ];

        return EpubFixtureFactory.CreateValid(
            packageEntryPath: OpfFixtureFactory.PackagePath,
            packageContent: package,
            additionalEntries: Merge(defaults, additionalEntries));
    }

    private static (string Path, string Content)[] Merge(
        (string Path, string Content)[] defaults,
        (string Path, string Content)[]? extras)
    {
        if (extras is null || extras.Length == 0)
        {
            return defaults;
        }

        Dictionary<string, string> values = defaults.ToDictionary(item => item.Path, item => item.Content, StringComparer.Ordinal);
        foreach ((string path, string content) in extras)
        {
            values[path] = content;
        }

        return values.Select(pair => (pair.Key, pair.Value)).ToArray();
    }
}
