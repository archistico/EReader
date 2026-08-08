using System.IO.Compression;
using System.Text;
using EbookReader.Cli;

namespace EbookReader.Cli.Tests;

public sealed class FirstReadableEpubTests
{
    [Fact]
    public void ValidEpubIsRenderedFromDomainReadingOrder()
    {
        string path = CreateReadableEpub();
        try
        {
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = CliEntryPoint.Run(["--plain", path], output, error);

            string text = NormalizeNewLines(output.ToString());
            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, error.ToString());
            Assert.Contains("Titolo CLI\n", text, StringComparison.Ordinal);
            Assert.Contains("di Emilie Rollandin\n", text, StringComparison.Ordinal);
            Assert.Contains("Capitolo Uno\n", text, StringComparison.Ordinal);
            Assert.Contains("Hello 😀 world.\n", text, StringComparison.Ordinal);
            Assert.Contains("> Citazione.\n", text, StringComparison.Ordinal);
            Assert.Contains("- Uno\n", text, StringComparison.Ordinal);
            Assert.Contains("1. Primo\n", text, StringComparison.Ordinal);
            Assert.Contains("codice\n  riga\n", text, StringComparison.Ordinal);
            Assert.Contains("[Immagine: Copertina — Didascalia]\n", text, StringComparison.Ordinal);
            Assert.Contains("---\n", text, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingFileUsesUsageExitCodeAndStderr()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ereader-missing-{Guid.NewGuid():N}.epub");
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = CliEntryPoint.Run(["--plain", path], output, error);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Contains("ER-CLI-FILE-001", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidEpubUsesInvalidPublicationExitCode()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ereader-invalid-{Guid.NewGuid():N}.epub");
        File.WriteAllBytes(path, [0x01, 0x02, 0x03]);

        try
        {
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = CliEntryPoint.Run(["--plain", path], output, error);

            Assert.Equal(3, exitCode);
            Assert.Equal(string.Empty, output.ToString());
            Assert.Contains("[DOCUMENT-UNREADABLE ER-EPUB-CONTAINER-", error.ToString(), StringComparison.Ordinal);
            Assert.Contains("[DOCUMENT_UNREADABLE] Impossibile aprire il libro in modo affidabile.", error.ToString(), StringComparison.Ordinal);
            Assert.Contains("lo stato di lettura esistente non viene aggiornato", error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EncryptedEpubUsesUnsupportedExitCodeAndIsNotRendered()
    {
        string path = CreateReadableEpub(encrypted: true);
        try
        {
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = CliEntryPoint.Run(["--plain", path], output, error);

            Assert.Equal(4, exitCode);
            Assert.Equal(string.Empty, output.ToString());
            Assert.Contains("[DOCUMENT-UNREADABLE ER-EPUB-PROTECTION-UNSUPPORTED-001]", error.ToString(), StringComparison.Ordinal);
            Assert.Contains("[DOCUMENT_UNREADABLE] Impossibile aprire il libro in modo affidabile.", error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void UnknownOptionUsesUsageExitCode()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = CliEntryPoint.Run(["--does-not-exist"], output, error);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Contains("Argomenti non validi", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void HelpDocumentsReadableEpubCommand()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = CliEntryPoint.Run(["--help"], output, error);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Contains("ereader <libro.epub>", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("ereader --plain <libro.epub>", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("M3.8 Diagnostics Foundation & Failure Taxonomy", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("nella libreria: / cerca, Esc cancella filtro", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("ereader --resume", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("/              cerca nel testo logico", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("n / N          risultato successivo/precedente", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("b              aggiunge/rimuove bookmark corrente", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("B              apre/chiude elenco bookmark", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("c              cambia tema", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("Enter          segue link/rimando nota corrente", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("Backspace      torna alla posizione precedente", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("F2             aggiunge/rimuove evidenziazione", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("F3             aggiunge/modifica nota personale", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("F4             apre/chiude elenco annotazioni", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("epub:type=\"noteref\"", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("ereader --config-path", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("ereader --init-config", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("EREADER_CONFIG_FILE", output.ToString(), StringComparison.Ordinal);
    }

    private static string CreateReadableEpub(bool encrypted = false)
    {
        string path = Path.Combine(Path.GetTempPath(), $"ereader-m10-{Guid.NewGuid():N}.epub");

        using FileStream file = File.Create(path);
        using ZipArchive archive = new(file, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: Encoding.UTF8);

        AddTextEntry(archive, "mimetype", "application/epub+zip", CompressionLevel.NoCompression);
        AddTextEntry(
            archive,
            "META-INF/container.xml",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
              <rootfiles>
                <rootfile full-path="EPUB/package.opf" media-type="application/oebps-package+xml" />
              </rootfiles>
            </container>
            """,
            CompressionLevel.Optimal);

        AddTextEntry(
            archive,
            "EPUB/package.opf",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="book-id">
              <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                <dc:identifier id="book-id">urn:uuid:11111111-2222-3333-4444-555555555555</dc:identifier>
                <dc:title>Titolo CLI</dc:title>
                <dc:language>it</dc:language>
                <dc:creator>Emilie Rollandin</dc:creator>
                <meta property="dcterms:modified">2026-08-07T17:00:00Z</meta>
              </metadata>
              <manifest>
                <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav" />
                <item id="c1" href="Text/ch1.xhtml" media-type="application/xhtml+xml" />
                <item id="img" href="images/pic.jpg" media-type="image/jpeg" />
              </manifest>
              <spine>
                <itemref idref="c1" />
              </spine>
            </package>
            """,
            CompressionLevel.Optimal);

        AddTextEntry(
            archive,
            "EPUB/nav.xhtml",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
              <head><title>Indice</title></head>
              <body>
                <nav epub:type="toc"><ol><li><a href="Text/ch1.xhtml#start">Capitolo Uno</a></li></ol></nav>
              </body>
            </html>
            """,
            CompressionLevel.Optimal);

        AddTextEntry(
            archive,
            "EPUB/Text/ch1.xhtml",
            """
            <!DOCTYPE html>
            <html xmlns="http://www.w3.org/1999/xhtml">
              <head><title>Capitolo Uno</title></head>
              <body>
                <h1 id="start">Capitolo Uno</h1>
                <p>Hello 😀 world.</p>
                <blockquote><p>Citazione.</p></blockquote>
                <ul><li>Uno</li></ul>
                <ol><li>Primo</li></ol>
                <pre>codice
              riga</pre>
                <figure><img src="../images/pic.jpg" alt="Copertina"/><figcaption>Didascalia</figcaption></figure>
                <hr/>
              </body>
            </html>
            """,
            CompressionLevel.Optimal);

        AddTextEntry(archive, "EPUB/images/pic.jpg", "not-a-real-jpeg", CompressionLevel.Optimal);

        if (encrypted)
        {
            AddTextEntry(
                archive,
                "META-INF/encryption.xml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <encryption xmlns="urn:oasis:names:tc:opendocument:xmlns:container"
                            xmlns:enc="http://www.w3.org/2001/04/xmlenc#">
                  <enc:EncryptedData>
                    <enc:EncryptionMethod Algorithm="http://www.w3.org/2001/04/xmlenc#aes256-cbc" />
                    <enc:CipherData><enc:CipherReference URI="EPUB/Text/ch1.xhtml" /></enc:CipherData>
                  </enc:EncryptedData>
                </encryption>
                """,
                CompressionLevel.Optimal);
        }

        return path;
    }

    private static void AddTextEntry(
        ZipArchive archive,
        string path,
        string content,
        CompressionLevel compressionLevel)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compressionLevel);
        using Stream stream = entry.Open();
        using StreamWriter writer = new(stream, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: false);
        writer.Write(content);
    }

    private static string NormalizeNewLines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
