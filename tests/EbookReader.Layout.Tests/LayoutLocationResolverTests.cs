using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Layout.Tests;

public sealed class LayoutLocationResolverTests
{
    [Fact]
    public void LocateMapsUtf16OffsetToWrappedVisualLine()
    {
        ParagraphBlock paragraph = new(new BlockId("p"), [new TextRun("alpha beta gamma")]);
        Book book = CreateBook(new ReadingSection(new SectionId("chapter"), [paragraph]));
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(6, 10));

        LayoutPosition beta = LayoutLocationResolver.Locate(
            book,
            layout,
            new ReadingLocation(new SectionId("chapter"), new BlockId("p"), 6));
        LayoutPosition gamma = LayoutLocationResolver.Locate(
            book,
            layout,
            new ReadingLocation(new SectionId("chapter"), new BlockId("p"), 11));

        Assert.Equal(new LayoutPosition(1, 1), beta);
        Assert.Equal(new LayoutPosition(1, 2), gamma);
        Assert.Equal("beta", layout.Pages[0].Lines[1].Text);
        Assert.Equal("gamma", layout.Pages[0].Lines[2].Text);
    }

    [Fact]
    public void LocateUsesUtf16OffsetsAcrossEmojiGrapheme()
    {
        ParagraphBlock paragraph = new(new BlockId("p"), [new TextRun("A 😀 B")]);
        Book book = CreateBook(new ReadingSection(new SectionId("chapter"), [paragraph]));
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(4, 10));

        LayoutPosition beforeEmoji = LayoutLocationResolver.Locate(
            book,
            layout,
            new ReadingLocation(new SectionId("chapter"), new BlockId("p"), 2));
        LayoutPosition finalWord = LayoutLocationResolver.Locate(
            book,
            layout,
            new ReadingLocation(new SectionId("chapter"), new BlockId("p"), 5));

        Assert.Equal(new LayoutPosition(1, 0), beforeEmoji);
        Assert.Equal(new LayoutPosition(1, 1), finalWord);
    }

    [Fact]
    public void SectionStartMapsToFirstReadableLineOfSection()
    {
        ReadingSection first = new(
            new SectionId("one"),
            [new ParagraphBlock(new BlockId("a"), [new TextRun("one")])]);
        ReadingSection second = new(
            new SectionId("two"),
            [new ParagraphBlock(new BlockId("b"), [new TextRun("two")])]);
        Book book = CreateBook(first, second);
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(40, 10));

        LayoutPosition position = LayoutLocationResolver.Locate(
            book,
            layout,
            ReadingLocation.AtSectionStart(second.Id));

        Assert.Equal("two", layout.Pages[position.PageNumber - 1].Lines[position.LineIndex].Text);
    }

    [Fact]
    public void BlockEndMapsToLastVisualLineOfBlock()
    {
        ParagraphBlock paragraph = new(new BlockId("p"), [new TextRun("alpha beta gamma")]);
        ReadingSection section = new(new SectionId("chapter"), [paragraph]);
        Book book = CreateBook(section);
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(6, 10));

        LayoutPosition position = LayoutLocationResolver.Locate(
            book,
            layout,
            new ReadingLocation(section.Id, paragraph.Id, "alpha beta gamma".Length));

        Assert.Equal("gamma", layout.Pages[position.PageNumber - 1].Lines[position.LineIndex].Text);
    }

    [Fact]
    public void SameLogicalLocationSurvivesReflowToDifferentViewport()
    {
        ParagraphBlock paragraph = new(
            new BlockId("p"),
            [new TextRun("alpha beta gamma delta epsilon zeta eta theta")]);
        ReadingSection section = new(new SectionId("chapter"), [paragraph]);
        Book book = CreateBook(section);
        ReadingLocation location = new(section.Id, paragraph.Id, 23);

        BookLayout narrow = DeterministicLayoutEngine.Layout(book, new LayoutViewport(10, 2));
        BookLayout wide = DeterministicLayoutEngine.Layout(book, new LayoutViewport(40, 10));
        LayoutPosition narrowPosition = LayoutLocationResolver.Locate(book, narrow, location);
        LayoutPosition widePosition = LayoutLocationResolver.Locate(book, wide, location);

        Assert.NotEqual(narrowPosition, widePosition);
        Assert.Equal(location, new ReadingLocation(section.Id, paragraph.Id, 23));
    }

    [Fact]
    public void VisualLinesExposeLogicalSourceRanges()
    {
        ParagraphBlock paragraph = new(new BlockId("p"), [new TextRun("alpha beta gamma")]);
        Book book = CreateBook(new ReadingSection(new SectionId("chapter"), [paragraph]));
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(6, 10));
        VisualLine[] lines = layout.Pages[0].Lines.Where(line => line.Kind != VisualLineKind.Spacing).ToArray();

        Assert.Collection(
            lines,
            line =>
            {
                Assert.Equal(0, line.SourceStartOffset);
                Assert.Equal(6, line.SourceEndOffset);
            },
            line =>
            {
                Assert.Equal(6, line.SourceStartOffset);
                Assert.Equal(11, line.SourceEndOffset);
            },
            line =>
            {
                Assert.Equal(11, line.SourceStartOffset);
                Assert.Equal(16, line.SourceEndOffset);
            });
    }

    [Fact]
    public void PreformattedTabsKeepLogicalOffsetMapping()
    {
        PreformattedBlock pre = new(new BlockId("pre"), "a\tb");
        ReadingSection section = new(new SectionId("chapter"), [pre]);
        Book book = CreateBook(section);
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(3, 10));

        LayoutPosition position = LayoutLocationResolver.Locate(book, layout, new ReadingLocation(section.Id, pre.Id, 1));

        Assert.Equal(2, layout.Pages[0].Lines.Count);
        Assert.Equal(new LayoutPosition(1, 1), position);
    }

    [Fact]
    public void EmptyBlockFallsForwardToNextReadableBlock()
    {
        ParagraphBlock empty = new(new BlockId("empty"));
        ParagraphBlock content = new(new BlockId("content"), [new TextRun("visible")]);
        ReadingSection section = new(new SectionId("chapter"), [empty, content]);
        Book book = CreateBook(section);
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(40, 10));

        LayoutPosition position = LayoutLocationResolver.Locate(
            book,
            layout,
            new ReadingLocation(section.Id, empty.Id, 0));

        Assert.Equal("visible", layout.Pages[position.PageNumber - 1].Lines[position.LineIndex].Text);
    }

    [Fact]
    public void GetLineStartSkipsSyntheticSpacing()
    {
        ParagraphBlock first = new(new BlockId("a"), [new TextRun("alpha")]);
        ParagraphBlock second = new(new BlockId("b"), [new TextRun("beta")]);
        ReadingSection section = new(new SectionId("chapter"), [first, second]);
        Book book = CreateBook(section);
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(40, 10));
        LayoutPosition spacing = new(1, 1);

        ReadingLocation location = LayoutLocationResolver.GetLineStart(layout, spacing);

        Assert.Equal(new ReadingLocation(section.Id, second.Id, 0), location);
    }

    private static Book CreateBook(params ReadingSection[] sections) =>
        new(new BookId("book"), new BookMetadata("Book"), sections);
}
