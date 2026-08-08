using EbookReader.Epub.Tests.Container;
using EbookReader.Epub.Tests.Package;

namespace EbookReader.Epub.Tests.Validation;

internal static class ValidationFixtureFactory
{
    public const string ChapterPath = "EPUB/Text/ch1.xhtml";
    public const string FontPath = "EPUB/fonts/test.ttf";

    public static MemoryStream Create(
        string? encryptionXml = null,
        bool includeRights = false,
        bool includeFontFile = false,
        bool declareFont = false,
        string fontMediaType = "font/ttf",
        string? packageContent = null,
        string? navigationContent = null,
        string? chapterContent = null,
        string chapterMediaType = "application/xhtml+xml",
        bool includeNavigationFile = true,
        bool includeChapterFile = true,
        (string Path, string Content)[]? additionalEntries = null)
    {
        string fontManifestItem = declareFont
            ? $"<item id=\"font\" href=\"fonts/test.ttf\" media-type=\"{fontMediaType}\" />"
            : string.Empty;
        string manifest = $"""
        <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav" />
        <item id="c1" href="Text/ch1.xhtml" media-type="{chapterMediaType}" />
        {fontManifestItem}
        """;

        string package = packageContent ?? OpfFixtureFactory.CreateEpub3Package(
            manifest: manifest,
            spine: "<itemref idref=\"c1\" />");

        string navigation = navigationContent ?? """
        <?xml version="1.0" encoding="UTF-8"?>
        <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
          <head><title>Navigation</title></head>
          <body><nav epub:type="toc"><ol><li><a href="Text/ch1.xhtml#start">One</a></li></ol></nav></body>
        </html>
        """;

        string chapter = chapterContent ?? """
        <!DOCTYPE html>
        <html xmlns="http://www.w3.org/1999/xhtml">
          <head><title>One</title></head>
          <body><h1 id="start">One</h1><p>Hello world.</p></body>
        </html>
        """;

        List<(string Path, string Content)> entries = [];

        if (includeNavigationFile)
        {
            entries.Add(("EPUB/nav.xhtml", navigation));
        }

        if (includeChapterFile)
        {
            entries.Add((ChapterPath, chapter));
        }

        if (includeFontFile)
        {
            entries.Add((FontPath, "fake-font-data"));
        }

        if (encryptionXml is not null)
        {
            entries.Add(("META-INF/encryption.xml", encryptionXml));
        }

        if (includeRights)
        {
            entries.Add(("META-INF/rights.xml", "<rights xmlns=\"urn:example:rights\" />"));
        }

        if (additionalEntries is not null)
        {
            entries.AddRange(additionalEntries);
        }

        return EpubFixtureFactory.CreateValid(
            packageContent: package,
            additionalEntries: entries);
    }

    public static string EncryptionXml(
        string resourcePath,
        string? algorithm = "http://www.idpf.org/2008/embedding")
    {
        string encryptionMethod = algorithm is null
            ? string.Empty
            : $"<enc:EncryptionMethod Algorithm=\"{algorithm}\" />";

        return $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <encryption xmlns="urn:oasis:names:tc:opendocument:xmlns:container"
                    xmlns:enc="http://www.w3.org/2001/04/xmlenc#">
          <enc:EncryptedData>
            {encryptionMethod}
            <enc:CipherData><enc:CipherReference URI="{resourcePath}" /></enc:CipherData>
          </enc:EncryptedData>
        </encryption>
        """;
    }
}
