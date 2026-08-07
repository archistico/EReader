using EbookReader.Domain.Books;
using EbookReader.Epub.Container;
using EbookReader.Epub.Content;
using EbookReader.Epub.Tests.Package;

namespace EbookReader.Epub.Tests.Content;

internal static class ContentFixtureFactory
{
    public static Book ReadBook(
        string chapter1,
        string? chapter2 = null,
        string? navigationItems = null,
        string? metadata = null,
        string? manifest = null,
        string? spine = null,
        (string Path, string Content)[]? extraEntries = null)
    {
        using MemoryStream stream = Create(
            chapter1,
            chapter2,
            navigationItems,
            metadata,
            manifest,
            spine,
            extraEntries);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        return EpubBookReader.Read(container);
    }

    public static Book ReadEpub2(string chapter)
    {
        const string ncx = """
        <?xml version="1.0" encoding="UTF-8"?>
        <ncx xmlns="http://www.daisy.org/z3986/2005/ncx/" version="2005-1">
          <head><meta name="dtb:uid" content="book-id" /></head>
          <docTitle><text>Test</text></docTitle>
          <navMap><navPoint id="n1"><navLabel><text>One</text></navLabel><content src="Text/ch1.xhtml#start" /></navPoint></navMap>
        </ncx>
        """;

        using MemoryStream stream = OpfFixtureFactory.CreateEpub2(
            additionalEntries:
            [
                ("EPUB/toc.ncx", ncx),
                ("EPUB/Text/ch1.xhtml", chapter),
            ]);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        return EpubBookReader.Read(container);
    }

    public static EpubContentException ReadFailure(
        string chapter1,
        string? navigationItems = null,
        string? manifest = null,
        string? spine = null,
        (string Path, string Content)[]? extraEntries = null)
    {
        using MemoryStream stream = Create(
            chapter1,
            chapter2: null,
            navigationItems,
            metadata: null,
            manifest,
            spine,
            extraEntries);
        using EpubContainer container = EpubContainer.Open(stream, leaveOpen: true);
        return Assert.Throws<EpubContentException>(() => EpubBookReader.Read(container));
    }

    private static MemoryStream Create(
        string chapter1,
        string? chapter2,
        string? navigationItems,
        string? metadata,
        string? manifest,
        string? spine,
        (string Path, string Content)[]? extraEntries)
    {
        string nav = $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
          <head><title>Navigation</title></head>
          <body>
            <nav epub:type="toc"><ol>{navigationItems ?? "<li><a href=\"Text/ch1.xhtml#start\">Chapter One</a></li>"}</ol></nav>
          </body>
        </html>
        """;

        string defaultManifest = chapter2 is null
            ? "<item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" /><item id=\"img\" href=\"images/pic.jpg\" media-type=\"image/jpeg\" /><item id=\"css\" href=\"styles/book.css\" media-type=\"text/css\" />"
            : "<item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\" /><item id=\"c1\" href=\"Text/ch1.xhtml\" media-type=\"application/xhtml+xml\" /><item id=\"c2\" href=\"Text/ch2.xhtml\" media-type=\"application/xhtml+xml\" /><item id=\"img\" href=\"images/pic.jpg\" media-type=\"image/jpeg\" /><item id=\"css\" href=\"styles/book.css\" media-type=\"text/css\" />";

        string defaultSpine = chapter2 is null
            ? "<itemref idref=\"c1\" />"
            : "<itemref idref=\"c1\" /><itemref idref=\"c2\" linear=\"no\" />";

        Dictionary<string, string> entries = new(StringComparer.Ordinal)
        {
            ["EPUB/nav.xhtml"] = nav,
            ["EPUB/Text/ch1.xhtml"] = chapter1,
            ["EPUB/images/pic.jpg"] = "fake-image",
            ["EPUB/styles/book.css"] = "body { color: black; }",
        };
        if (chapter2 is not null)
        {
            entries["EPUB/Text/ch2.xhtml"] = chapter2;
        }

        if (extraEntries is not null)
        {
            foreach ((string path, string content) in extraEntries)
            {
                entries[path] = content;
            }
        }

        return OpfFixtureFactory.CreateEpub3(
            metadata: metadata,
            manifest: manifest ?? defaultManifest,
            spine: spine ?? defaultSpine,
            additionalEntries: entries.Select(pair => (pair.Key, pair.Value)).ToArray());
    }
}
