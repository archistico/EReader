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


    [Theory]
    [InlineData("http://example.com/")]
    [InlineData("https://example.com/")]
    [InlineData("mailto:reader@example.com")]
    public void ExternalLinkPolicyAllowsOnlyExplicitSchemes(string value)
    {
        Assert.True(ExternalLinkPolicy.IsAllowed(new Uri(value)));
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/plain,hello")]
    [InlineData("ftp://example.com/file")]
    [InlineData("shell:open")]
    public void ExternalLinkPolicyRejectsLocalScriptAndUnknownSchemes(string value)
    {
        Assert.False(ExternalLinkPolicy.IsAllowed(new Uri(value)));
    }

    [Fact]
    public void ExternalLinkPolicyRejectsRelativeUri()
    {
        Assert.False(ExternalLinkPolicy.IsAllowed(new Uri("chapter.xhtml", UriKind.Relative)));
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
