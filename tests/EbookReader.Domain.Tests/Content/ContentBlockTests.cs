namespace EbookReader.Domain.Tests.Content;

public sealed class ContentBlockTests
{
    [Fact]
    public void HeadingLevelMustBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new HeadingBlock(new BlockId("h"), 0, [new TextRun("Heading")]));
    }

    [Fact]
    public void QuoteDepthMustBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new QuoteBlock(new BlockId("q"), [new TextRun("Quote")], depth: 0));
    }

    [Fact]
    public void UnorderedListCannotHaveOrdinal()
    {
        Assert.Throws<ArgumentException>(
            () => new ListItemBlock(
                new BlockId("li"),
                ListKind.Unordered,
                [new TextRun("Item")],
                ordinal: 1));
    }

    [Fact]
    public void OrderedListMayCarryOrdinal()
    {
        ListItemBlock item = new(
            new BlockId("li"),
            ListKind.Ordered,
            [new TextRun("Item")],
            depth: 2,
            ordinal: 4);

        Assert.Equal(2, item.Depth);
        Assert.Equal(4, item.Ordinal);
    }

    [Fact]
    public void ImagePlainTextIncludesAlternativeTextAndCaption()
    {
        ImageBlock image = new(new BlockId("img"), new ResourceId("r1"), "Alt", "Caption");

        Assert.Equal("Alt\nCaption", ContentText.GetPlainText(image));
    }

    [Fact]
    public void PreformattedTextIsNotNormalized()
    {
        PreformattedBlock block = new(new BlockId("pre"), "  A\n   B");

        Assert.Equal("  A\n   B", ContentText.GetPlainText(block));
    }

    [Fact]
    public void ListRejectsUndefinedKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ListItemBlock(new BlockId("li"), (ListKind)999, [new TextRun("Item")]));
    }

    [Fact]
    public void ThematicBreakHasNoLogicalText()
    {
        ThematicBreakBlock block = new(new BlockId("hr"));

        Assert.Equal(string.Empty, ContentText.GetPlainText(block));
    }
}
