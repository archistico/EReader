using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Layout.Tests;

public sealed class LayoutNavigatorTests
{
    [Fact]
    public void NextAndPreviousLineReturnLogicalLocations()
    {
        ParagraphBlock paragraph = new(new BlockId("p"), [new TextRun("alpha beta gamma")]);
        ReadingSection section = new(new SectionId("chapter"), [paragraph]);
        Book book = CreateBook(section);
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(6, 10));
        ReadingLocation start = new(section.Id, paragraph.Id, 0);

        ReadingLocation? second = LayoutNavigator.NextLine(book, layout, start);
        ReadingLocation? third = LayoutNavigator.NextLine(book, layout, second!);
        ReadingLocation? back = LayoutNavigator.PreviousLine(book, layout, third!);

        Assert.Equal(new ReadingLocation(section.Id, paragraph.Id, 6), second);
        Assert.Equal(new ReadingLocation(section.Id, paragraph.Id, 11), third);
        Assert.Equal(second, back);
    }

    [Fact]
    public void LineNavigationReturnsNullAtBookBoundaries()
    {
        ParagraphBlock paragraph = new(new BlockId("p"), [new TextRun("alpha beta")]);
        ReadingSection section = new(new SectionId("chapter"), [paragraph]);
        Book book = CreateBook(section);
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(6, 10));

        Assert.Null(LayoutNavigator.PreviousLine(book, layout, new ReadingLocation(section.Id, paragraph.Id, 0)));
        Assert.Null(LayoutNavigator.NextLine(book, layout, new ReadingLocation(section.Id, paragraph.Id, 6)));
    }

    [Fact]
    public void PageNavigationReturnsFirstLogicalLineOfAdjacentPage()
    {
        ParagraphBlock paragraph = new(
            new BlockId("p"),
            [new TextRun("one two three four five six seven eight nine ten")]);
        ReadingSection section = new(new SectionId("chapter"), [paragraph]);
        Book book = CreateBook(section);
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(8, 2));
        ReadingLocation start = new(section.Id, paragraph.Id, 0);

        ReadingLocation? next = LayoutNavigator.NextPage(book, layout, start);
        ReadingLocation? previous = LayoutNavigator.PreviousPage(book, layout, next!);

        Assert.NotNull(next);
        Assert.NotEqual(start, next);
        Assert.Equal(start, previous);
    }

    [Fact]
    public void PageNavigationReturnsNullBeyondFirstAndLastPage()
    {
        ParagraphBlock paragraph = new(new BlockId("p"), [new TextRun("one two three four")]);
        ReadingSection section = new(new SectionId("chapter"), [paragraph]);
        Book book = CreateBook(section);
        BookLayout layout = DeterministicLayoutEngine.Layout(book, new LayoutViewport(8, 1));
        ReadingLocation start = new(section.Id, paragraph.Id, 0);
        ReadingLocation last = LayoutLocationResolver.GetLineStart(
            layout,
            new LayoutPosition(layout.Pages.Count, 0));

        Assert.Null(LayoutNavigator.PreviousPage(book, layout, start));
        Assert.Null(LayoutNavigator.NextPage(book, layout, last));
    }

    private static Book CreateBook(params ReadingSection[] sections) =>
        new(new BookId("book"), new BookMetadata("Book"), sections);
}
