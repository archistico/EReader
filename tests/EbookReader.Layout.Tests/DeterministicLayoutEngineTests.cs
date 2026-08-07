using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Resources;

namespace EbookReader.Layout.Tests;

public sealed class DeterministicLayoutEngineTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 10)]
    [InlineData(-1, 10)]
    [InlineData(10, 0)]
    [InlineData(10, -1)]
    public void ViewportRequiresPositiveDimensions(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayoutViewport(width, height));
    }

    [Fact]
    public void CellWidthUsesGraphemeClustersAndWideRunes()
    {
        Assert.Equal(6, TerminalCellWidth.Measure("A😀日e\u0301"));
    }

    [Fact]
    public void FlowWrappingDoesNotSplitEmojiGrapheme()
    {
        ReadingSection section = Section(new ParagraphBlock(Id("p"), [new TextRun("A 😀 B")]));

        BookLayout layout = DeterministicLayoutEngine.Layout(section, new LayoutViewport(4, 10));
        VisualLine[] content = layout.Pages.SelectMany(page => page.Lines)
            .Where(line => line.Kind != VisualLineKind.Spacing)
            .ToArray();

        Assert.Equal(["A 😀", "B"], content.Select(line => line.Text));
        Assert.All(content, line => Assert.True(line.DisplayWidth <= 4));
    }

    [Fact]
    public void HeadingRetainsSemanticKindLevelAndSourceIds()
    {
        HeadingBlock heading = new(Id("heading"), 2, [new TextRun("Titolo")]);
        ReadingSection section = Section(heading);

        VisualLine line = Assert.Single(DeterministicLayoutEngine.Layout(section, new LayoutViewport(40, 10)).Pages[0].Lines);

        Assert.Equal("Titolo", line.Text);
        Assert.Equal(VisualLineKind.Heading, line.Kind);
        Assert.Equal(2, line.HeadingLevel);
        Assert.Equal(section.Id, line.SectionId);
        Assert.Equal(heading.Id, line.BlockId);
    }

    [Fact]
    public void QuoteAndListPrefixesRepeatOnWrappedLines()
    {
        ReadingSection section = Section(
            new QuoteBlock(Id("quote"), [new TextRun("alpha beta gamma")], depth: 2),
            new ListItemBlock(
                Id("list"),
                ListKind.Ordered,
                [new TextRun("delta epsilon")],
                depth: 2,
                ordinal: 7));

        VisualLine[] lines = DeterministicLayoutEngine.Layout(section, new LayoutViewport(12, 20)).Pages
            .SelectMany(page => page.Lines)
            .Where(line => line.Kind != VisualLineKind.Spacing)
            .ToArray();

        Assert.Equal(["> > alpha", "> > beta", "> > gamma", "  7. delta", "     epsilon"], lines.Select(line => line.Text));
    }

    [Fact]
    public void PreformattedTextPreservesSpacesAndExpandsTabsDeterministically()
    {
        ReadingSection section = Section(new PreformattedBlock(Id("pre"), "a\tb\n  c"));

        VisualLine[] lines = DeterministicLayoutEngine.Layout(section, new LayoutViewport(8, 10)).Pages[0].Lines.ToArray();

        Assert.Equal(["a   b", "  c"], lines.Select(line => line.Text));
        Assert.All(lines, line => Assert.Equal(VisualLineKind.Preformatted, line.Kind));
    }


    [Fact]
    public void InlineStrongAndEmphasisSurviveLayoutAsSemanticSpans()
    {
        ReadingSection section = Section(
            new ParagraphBlock(
                Id("styled"),
                [
                    new TextRun("plain "),
                    new StrongSpan([new TextRun("bold")]),
                    new TextRun(" "),
                    new EmphasisSpan([new TextRun("italic")]),
                ]));

        VisualLine line = Assert.Single(DeterministicLayoutEngine.Layout(section, new LayoutViewport(80, 10)).Pages[0].Lines);

        Assert.Equal("plain bold italic", line.Text);
        Assert.Collection(
            line.StyleSpans,
            span =>
            {
                Assert.Equal(6, span.StartIndex);
                Assert.Equal(4, span.Length);
                Assert.Equal(VisualTextStyle.Strong, span.Style);
            },
            span =>
            {
                Assert.Equal(11, span.StartIndex);
                Assert.Equal(6, span.Length);
                Assert.Equal(VisualTextStyle.Emphasis, span.Style);
            });
    }

    [Fact]
    public void NestedStrongAndEmphasisPreserveCombinedSemanticStyle()
    {
        ReadingSection section = Section(
            new ParagraphBlock(
                Id("nested"),
                [new StrongSpan([new TextRun("bold "), new EmphasisSpan([new TextRun("both")])])]));

        VisualLine line = Assert.Single(DeterministicLayoutEngine.Layout(section, new LayoutViewport(80, 10)).Pages[0].Lines);

        Assert.Equal("bold both", line.Text);
        Assert.Collection(
            line.StyleSpans,
            span =>
            {
                Assert.Equal(0, span.StartIndex);
                Assert.Equal(4, span.Length);
                Assert.Equal(VisualTextStyle.Strong, span.Style);
            },
            span =>
            {
                Assert.Equal(5, span.StartIndex);
                Assert.Equal(4, span.Length);
                Assert.Equal(VisualTextStyle.Strong | VisualTextStyle.Emphasis, span.Style);
            });
    }

    [Fact]
    public void StyledTextSurvivesWrappingWithLineLocalOffsets()
    {
        ReadingSection section = Section(
            new ParagraphBlock(
                Id("wrapped-style"),
                [new TextRun("alpha "), new StrongSpan([new TextRun("beta gamma")])]));

        VisualLine[] lines = DeterministicLayoutEngine.Layout(section, new LayoutViewport(6, 10)).Pages[0].Lines.ToArray();

        Assert.Equal(["alpha", "beta", "gamma"], lines.Select(line => line.Text));
        Assert.Empty(lines[0].StyleSpans);
        Assert.All(lines.Skip(1), line =>
        {
            VisualTextSpan span = Assert.Single(line.StyleSpans);
            Assert.Equal(0, span.StartIndex);
            Assert.Equal(line.Text.Length, span.Length);
            Assert.Equal(VisualTextStyle.Strong, span.Style);
        });
    }

    [Fact]
    public void QuotePrefixRemainsPlainWhileStrongContentKeepsStyle()
    {
        ReadingSection section = Section(
            new QuoteBlock(Id("quote-style"), [new StrongSpan([new TextRun("bold")])], depth: 1));

        VisualLine line = Assert.Single(DeterministicLayoutEngine.Layout(section, new LayoutViewport(20, 10)).Pages[0].Lines);
        VisualTextSpan span = Assert.Single(line.StyleSpans);

        Assert.Equal("> bold", line.Text);
        Assert.Equal(2, span.StartIndex);
        Assert.Equal(4, span.Length);
        Assert.Equal(VisualTextStyle.Strong, span.Style);
    }

    [Fact]
    public void StyledEmojiUsesUtf16SpanLengthWithoutSplittingGrapheme()
    {
        ReadingSection section = Section(
            new ParagraphBlock(Id("emoji-style"), [new StrongSpan([new TextRun("😀")])]));

        VisualLine line = Assert.Single(DeterministicLayoutEngine.Layout(section, new LayoutViewport(10, 10)).Pages[0].Lines);
        VisualTextSpan span = Assert.Single(line.StyleSpans);

        Assert.Equal("😀", line.Text);
        Assert.Equal(0, span.StartIndex);
        Assert.Equal(2, span.Length);
        Assert.Equal(VisualTextStyle.Strong, span.Style);
    }

    [Fact]
    public void PaginationHonorsHeightAndNeverStartsWithSpacing()
    {
        ReadingSection section = Section(
            new ParagraphBlock(Id("one"), [new TextRun("one two three four")]),
            new ParagraphBlock(Id("two"), [new TextRun("five six seven eight")]),
            new ImageBlock(Id("image"), new ResourceId("cover"), "Cover"),
            new ThematicBreakBlock(Id("break")));

        BookLayout layout = DeterministicLayoutEngine.Layout(section, new LayoutViewport(10, 3));

        Assert.True(layout.Pages.Count > 1);
        Assert.All(layout.Pages, page => Assert.InRange(page.Lines.Count, 1, 3));
        Assert.All(layout.Pages, page => Assert.NotEqual(VisualLineKind.Spacing, page.Lines[0].Kind));
        Assert.All(layout.Pages.SelectMany(page => page.Lines), line => Assert.True(line.DisplayWidth <= 10));
    }

    private static ReadingSection Section(params ContentBlock[] blocks) =>
        new(new SectionId("section"), blocks);

    private static BlockId Id(string value) => new(value);
}
