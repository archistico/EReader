namespace EbookReader.Domain.Tests.Content;

public sealed class InlineContentTests
{
    [Fact]
    public void TextRunPreservesSignificantWhitespace()
    {
        TextRun text = new(" hello ");

        Assert.Equal(" hello ", text.Text);
    }

    [Fact]
    public void TextRunRejectsEmptyString()
    {
        Assert.Throws<ArgumentException>(() => new TextRun(string.Empty));
    }

    [Fact]
    public void InlineContainerSnapshotsChildren()
    {
        List<InlineContent> children = [new TextRun("one")];
        StrongSpan strong = new(children);

        children.Add(new TextRun("two"));

        Assert.Single(strong.Content);
    }

    [Fact]
    public void ExternalLinkRequiresAbsoluteUri()
    {
        Uri relative = new("chapter2", UriKind.Relative);

        Assert.Throws<ArgumentException>(() => new ExternalLinkTarget(relative));
    }

    [Fact]
    public void InternalLinkStoresLogicalLocation()
    {
        ReadingLocation location = ReadingLocation.AtBlockStart(new SectionId("s1"), new BlockId("p1"));
        InternalLinkTarget target = new(location);

        Assert.Equal(location, target.Location);
    }

    [Fact]
    public void HyperlinkDefaultsToGenericRole()
    {
        HyperlinkSpan link = new(
            new ExternalLinkTarget(new Uri("https://example.com/")),
            [new TextRun("Example")]);

        Assert.Equal(HyperlinkRole.Generic, link.Role);
    }

    [Fact]
    public void HyperlinkStoresNoteReferenceRole()
    {
        ReadingLocation location = ReadingLocation.AtBlockStart(new SectionId("s1"), new BlockId("note"));
        HyperlinkSpan link = new(
            new InternalLinkTarget(location),
            [new TextRun("1")],
            HyperlinkRole.NoteReference);

        Assert.Equal(HyperlinkRole.NoteReference, link.Role);
    }

    [Fact]
    public void PlainTextFlattensNestedFormattingAndBreaks()
    {
        InlineContent[] content =
        [
            new TextRun("Hello "),
            new StrongSpan([new TextRun("bold"), LineBreakInline.Instance]),
            new EmphasisSpan([new TextRun("world")]),
        ];

        Assert.Equal("Hello bold\nworld", ContentText.GetPlainText(content));
    }
}
