using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Resources;
using EbookReader.Epub.Content;

namespace EbookReader.Epub.Tests.Content;

public sealed class EpubBookReaderTests
{
    private const string BasicChapter = """
        <html xmlns="http://www.w3.org/1999/xhtml">
          <head><title>Document title</title></head>
          <body>
            <h1 id="start">Chapter One</h1>
            <p>Hello <strong>bold</strong> and <em>italic</em>.</p>
          </body>
        </html>
        """;

    [Fact]
    public void ReadBuildsFormatNeutralBookFromSpine()
    {
        Book book = ContentFixtureFactory.ReadBook(
            BasicChapter,
            "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><h1 id=\"two\">Two</h1></body></html>",
            "<li><a href=\"Text/ch1.xhtml#start\">One</a></li><li><a href=\"Text/ch2.xhtml#two\">Two</a></li>");

        Assert.Equal("Titolo di prova", book.Metadata.Title);
        Assert.Equal(2, book.ReadingOrder.Count);
        Assert.Equal(ReadingSectionRole.Primary, book.ReadingOrder[0].Role);
        Assert.Equal(ReadingSectionRole.Supplementary, book.ReadingOrder[1].Role);
        Assert.Equal("Chapter One", book.ReadingOrder[0].Title);
    }

    [Fact]
    public void ReadSupportsEpub2NcxAndXhtmlContent()
    {
        Book book = ContentFixtureFactory.ReadEpub2(BasicChapter);

        Assert.Single(book.ReadingOrder);
        Assert.Equal("Chapter One", book.ReadingOrder[0].Title);
        Assert.Single(book.TableOfContents.Items);
        Assert.NotNull(book.TableOfContents.Items[0].Target);
    }

    [Fact]
    public void ReadMapsHeadingParagraphStrongAndEmphasis()
    {
        Book book = ContentFixtureFactory.ReadBook(BasicChapter);
        ReadingSection section = book.ReadingOrder[0];

        HeadingBlock heading = Assert.IsType<HeadingBlock>(section.Blocks[0]);
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(section.Blocks[1]);
        Assert.Equal(1, heading.Level);
        Assert.Contains(paragraph.Content, item => item is StrongSpan);
        Assert.Contains(paragraph.Content, item => item is EmphasisSpan);
        Assert.Equal("Hello bold and italic.", ContentText.GetPlainText(paragraph));
    }

    [Fact]
    public void ReadNormalizesFlowWhitespaceDeterministically()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1>
          <p>  Alpha
             beta <span> gamma </span> delta  </p>
        </body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(chapter);
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(book.ReadingOrder[0].Blocks[1]);
        Assert.Equal("Alpha beta gamma delta", ContentText.GetPlainText(paragraph));
    }

    [Fact]
    public void ReadPreservesPreformattedWhitespace()
    {
        const string chapter = "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body><h1 id=\"start\">One</h1><pre>  alpha\n    beta</pre></body></html>";

        Book book = ContentFixtureFactory.ReadBook(chapter);
        PreformattedBlock pre = Assert.IsType<PreformattedBlock>(book.ReadingOrder[0].Blocks[1]);
        Assert.Equal("  alpha\n    beta", pre.Text);
    }

    [Fact]
    public void ReadMapsListsQuotesAndThematicBreaks()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1>
          <ol start="3"><li>Third</li><li value="8">Eighth<ul><li>Nested</li></ul></li></ol>
          <blockquote>Quoted text</blockquote><hr />
        </body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(chapter);
        ReadingSection section = book.ReadingOrder[0];
        ListItemBlock third = Assert.IsType<ListItemBlock>(section.Blocks[1]);
        ListItemBlock eighth = Assert.IsType<ListItemBlock>(section.Blocks[2]);
        ListItemBlock nested = Assert.IsType<ListItemBlock>(section.Blocks[3]);
        Assert.Equal(3, third.Ordinal);
        Assert.Equal(8, eighth.Ordinal);
        Assert.Equal(2, nested.Depth);
        Assert.IsType<QuoteBlock>(section.Blocks[4]);
        Assert.IsType<ThematicBreakBlock>(section.Blocks[5]);
    }

    [Fact]
    public void ReadMapsManifestImageToDomainResource()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><figure id="fig"><img src="../images/pic.jpg" alt="Picture" /><figcaption>Caption</figcaption></figure>
        </body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(chapter);
        ImageBlock image = Assert.IsType<ImageBlock>(book.ReadingOrder[0].Blocks[1]);
        Assert.Equal("img", image.ResourceId.Value);
        Assert.Equal("Picture", image.AlternativeText);
        Assert.Equal("Caption", image.Caption);
        Assert.Contains(book.Resources, resource => resource.Id.Value == "img" && resource.Kind == ResourceKind.Image);
        Assert.Contains(book.Resources, resource => resource.Id.Value == "css" && resource.Kind == ResourceKind.Stylesheet);
    }

    [Fact]
    public void ReadResolvesInternalLinkAcrossSections()
    {
        const string chapter1 = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><p><a href="ch2.xhtml#target">Go two</a></p>
        </body></html>
        """;
        const string chapter2 = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body><h1 id="target">Two</h1></body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(
            chapter1,
            chapter2,
            "<li><a href=\"Text/ch1.xhtml#start\">One</a></li><li><a href=\"Text/ch2.xhtml#target\">Two</a></li>");
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(book.ReadingOrder[0].Blocks[1]);
        HyperlinkSpan link = Assert.IsType<HyperlinkSpan>(Assert.Single(paragraph.Content));
        InternalLinkTarget target = Assert.IsType<InternalLinkTarget>(link.Target);
        Assert.Equal(book.ReadingOrder[1].Id, target.Location.SectionId);
        Assert.Equal(book.ReadingOrder[1].Blocks[0].Id, target.Location.BlockId);
    }

    [Fact]
    public void ReadMapsInlineAnchorToUtf16CharacterOffset()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1>
          <p><a href="#middle">jump</a></p>
          <p>Alpha <span id="middle">beta</span> gamma</p>
        </body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(chapter);
        ParagraphBlock linkParagraph = Assert.IsType<ParagraphBlock>(book.ReadingOrder[0].Blocks[1]);
        HyperlinkSpan link = Assert.IsType<HyperlinkSpan>(Assert.Single(linkParagraph.Content));
        InternalLinkTarget target = Assert.IsType<InternalLinkTarget>(link.Target);
        Assert.Equal(book.ReadingOrder[0].Blocks[2].Id, target.Location.BlockId);
        Assert.Equal(6, target.Location.CharacterOffset);
    }

    [Fact]
    public void ReadCountsUtf16CodeUnitsForLogicalOffsets()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1>
          <p><a href="#middle">jump</a></p>
          <p>😀 <span id="middle">beta</span></p>
        </body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(chapter);
        HyperlinkSpan link = Assert.IsType<HyperlinkSpan>(Assert.Single(Assert.IsType<ParagraphBlock>(book.ReadingOrder[0].Blocks[1]).Content));
        InternalLinkTarget target = Assert.IsType<InternalLinkTarget>(link.Target);
        Assert.Equal(3, target.Location.CharacterOffset);
    }

    [Fact]
    public void ReadMapsExternalHttpLinkWithoutNetworkAccess()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><p><a href="https://example.com/page">Example</a></p>
        </body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(chapter);
        HyperlinkSpan link = Assert.IsType<HyperlinkSpan>(Assert.Single(Assert.IsType<ParagraphBlock>(book.ReadingOrder[0].Blocks[1]).Content));
        ExternalLinkTarget target = Assert.IsType<ExternalLinkTarget>(link.Target);
        Assert.Equal("https://example.com/page", target.Uri.AbsoluteUri);
    }

    [Fact]
    public void ReadMapsLineBreakAsLogicalNewline()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><p>Alpha<br />Beta</p>
        </body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(chapter);
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(book.ReadingOrder[0].Blocks[1]);
        Assert.Equal("Alpha\nBeta", ContentText.GetPlainText(paragraph));
        Assert.Contains(paragraph.Content, item => ReferenceEquals(item, LineBreakInline.Instance));
    }

    [Fact]
    public void ReadDropsJavascriptLinkSemanticsButPreservesText()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><p><a href="javascript:alert(1)">Do not execute</a></p>
        </body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(chapter);
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(book.ReadingOrder[0].Blocks[1]);
        Assert.Equal("Do not execute", ContentText.GetPlainText(paragraph));
        Assert.DoesNotContain(paragraph.Content, item => item is HyperlinkSpan);
    }

    [Fact]
    public void ReadDropsFileLinkSemanticsButPreservesText()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><p><a href="file:///etc/passwd">Local file</a></p>
        </body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(chapter);
        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(book.ReadingOrder[0].Blocks[1]);
        Assert.Equal("Local file", ContentText.GetPlainText(paragraph));
        Assert.DoesNotContain(paragraph.Content, item => item is HyperlinkSpan);
    }

    [Fact]
    public void ReadPreservesTargetlessNavigationGroupingNode()
    {
        const string nav = """
        <li><span>Part One</span><ol><li><a href="Text/ch1.xhtml#start">Chapter One</a></li></ol></li>
        """;

        Book book = ContentFixtureFactory.ReadBook(BasicChapter, navigationItems: nav);
        EbookReader.Domain.Navigation.NavigationItem group = Assert.Single(book.TableOfContents.Items);
        Assert.Null(group.Target);
        Assert.Single(group.Children);
        Assert.NotNull(group.Children[0].Target);
    }

    [Fact]
    public void ReadMapsPackageCreatorsIdentifiersAndSubjects()
    {
        const string metadata = """
        <dc:identifier id="book-id" opf:scheme="ISBN" xmlns:opf="http://www.idpf.org/2007/opf">9780000000001</dc:identifier>
        <dc:title>Book</dc:title><dc:language>en</dc:language>
        <dc:creator opf:role="aut" opf:file-as="Doe, Jane" xmlns:opf="http://www.idpf.org/2007/opf">Jane Doe</dc:creator>
        <dc:contributor opf:role="trl" xmlns:opf="http://www.idpf.org/2007/opf">Mario Rossi</dc:contributor>
        <dc:subject>Fiction</dc:subject><dc:publisher>Press</dc:publisher><dc:rights>Rights</dc:rights>
        <meta property="dcterms:modified">2026-08-07T14:00:00Z</meta>
        """;

        Book book = ContentFixtureFactory.ReadBook(BasicChapter, metadata: metadata);
        Assert.Equal("9780000000001", book.Id.Value);
        Assert.Contains(book.Metadata.Identifiers, value => value.Value == "9780000000001" && value.Scheme == "ISBN");
        Assert.Contains(book.Metadata.Contributors, value => value.Name == "Jane Doe" && value.Role == ContributorRole.Author);
        Assert.Contains(book.Metadata.Contributors, value => value.Name == "Mario Rossi" && value.Role == ContributorRole.Translator);
        Assert.Equal(["Fiction"], book.Metadata.Subjects);
        Assert.Equal("Press", book.Metadata.Publisher);
        Assert.Equal("Rights", book.Metadata.Rights);
    }

    [Fact]
    public void ReadRejectsMissingInternalAnchor()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body><h1 id="start">One</h1><p><a href="#missing">bad</a></p></body></html>
        """;

        EpubContentException exception = ContentFixtureFactory.ReadFailure(chapter);
        Assert.Equal(EpubContentErrorCode.InternalTargetNotFound, exception.ErrorCode);
    }

    [Fact]
    public void ReadRejectsDuplicateAnchor()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body><h1 id="start">One</h1><p id="dup">A</p><p id="dup">B</p></body></html>
        """;

        EpubContentException exception = ContentFixtureFactory.ReadFailure(chapter);
        Assert.Equal(EpubContentErrorCode.DuplicateAnchor, exception.ErrorCode);
    }

    [Fact]
    public void ReadRejectsImageOutsideManifest()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body><h1 id="start">One</h1><img src="../images/missing.jpg" alt="Missing" /></body></html>
        """;

        EpubContentException exception = ContentFixtureFactory.ReadFailure(chapter);
        Assert.Equal(EpubContentErrorCode.ImageResourceNotFound, exception.ErrorCode);
    }

    [Fact]
    public void ReadRejectsNonImageManifestResourceUsedAsImage()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body><h1 id="start">One</h1><img src="../styles/book.css" alt="Wrong" /></body></html>
        """;

        EpubContentException exception = ContentFixtureFactory.ReadFailure(chapter);
        Assert.Equal(EpubContentErrorCode.ImageResourceNotImage, exception.ErrorCode);
    }

    [Fact]
    public void ReadRejectsTraversalInLocalLink()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body><h1 id="start">One</h1><p><a href="../../../outside.xhtml">bad</a></p></body></html>
        """;

        EpubContentException exception = ContentFixtureFactory.ReadFailure(chapter);
        Assert.Equal(EpubContentErrorCode.InvalidLocalReference, exception.ErrorCode);
    }

    [Fact]
    public void ReadUsesXhtmlFallbackForNonXhtmlSpineItem()
    {
        const string manifest = """
        <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav" />
        <item id="primary" href="data.bin" media-type="application/octet-stream" fallback="c1" />
        <item id="c1" href="Text/ch1.xhtml" media-type="application/xhtml+xml" />
        <item id="img" href="images/pic.jpg" media-type="image/jpeg" />
        <item id="css" href="styles/book.css" media-type="text/css" />
        """;

        Book book = ContentFixtureFactory.ReadBook(
            BasicChapter,
            manifest: manifest,
            spine: "<itemref idref=\"primary\" />",
            extraEntries: [("EPUB/data.bin", "payload")]);

        Assert.Single(book.ReadingOrder);
        Assert.Equal("Chapter One", book.ReadingOrder[0].Title);
    }

    [Fact]
    public void ReadRejectsSpineWithoutXhtmlFallback()
    {
        const string manifest = """
        <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav" />
        <item id="primary" href="data.bin" media-type="application/octet-stream" />
        """;

        EpubContentException exception = ContentFixtureFactory.ReadFailure(
            BasicChapter,
            navigationItems: "<li><a href=\"data.bin\">Data</a></li>",
            manifest: manifest,
            spine: "<itemref idref=\"primary\" />",
            extraEntries: [("EPUB/data.bin", "payload")]);

        Assert.Equal(EpubContentErrorCode.UnsupportedSpineContent, exception.ErrorCode);
    }

    [Fact]
    public void ScriptStyleAndNavContentAreNotProjectedIntoReadingText()
    {
        const string chapter = """
        <html xmlns="http://www.w3.org/1999/xhtml"><body>
          <h1 id="start">One</h1><script>bad()</script><style>.x{}</style><nav>duplicate navigation</nav><p>Visible</p>
        </body></html>
        """;

        Book book = ContentFixtureFactory.ReadBook(chapter);
        Assert.Equal(2, book.ReadingOrder[0].Blocks.Count);
        Assert.Equal("Visible", ContentText.GetPlainText(book.ReadingOrder[0].Blocks[1]));
    }
}
