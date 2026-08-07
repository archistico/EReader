using EbookReader.Application.Progress;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Tests;

public sealed class BookProgressIndexTests
{
    [Fact]
    public void StartsAtZeroAtBeginningOfFirstSection()
    {
        Book book = CreateBook(
            new ReadingSection(new SectionId("one"), [Paragraph("p1", "alpha")]),
            new ReadingSection(new SectionId("two"), [Paragraph("p2", "beta")]));
        BookProgressIndex index = new(book);

        BookProgress progress = index.Locate(ReadingLocation.AtSectionStart(new SectionId("one")));

        Assert.Equal(0, progress.ConsumedUnits);
        Assert.Equal(9, progress.TotalUnits);
        Assert.Equal(0m, progress.Percentage);
    }

    [Fact]
    public void UsesUtf16CodeUnitsLikeReadingLocation()
    {
        Book book = CreateBook(
            new ReadingSection(new SectionId("one"), [Paragraph("p1", "A😀B")]),
            new ReadingSection(new SectionId("two"), [Paragraph("p2", "xy")]));
        BookProgressIndex index = new(book);

        BookProgress progress = index.Locate(new ReadingLocation(new SectionId("one"), new BlockId("p1"), 3));

        Assert.Equal(3, progress.ConsumedUnits);
        Assert.Equal(6, progress.TotalUnits);
        Assert.Equal(50m, progress.Percentage);
    }

    [Fact]
    public void SectionStartCountsAllPreviousLogicalText()
    {
        Book book = CreateBook(
            new ReadingSection(new SectionId("one"), [Paragraph("p1", "abcd")]),
            new ReadingSection(new SectionId("two"), [Paragraph("p2", "ef")]));
        BookProgressIndex index = new(book);

        BookProgress progress = index.Locate(ReadingLocation.AtSectionStart(new SectionId("two")));

        Assert.Equal(4, progress.ConsumedUnits);
        Assert.Equal(6, progress.TotalUnits);
    }

    [Fact]
    public void EndOfFinalBlockIsOneHundredPercent()
    {
        Book book = CreateBook(
            new ReadingSection(new SectionId("one"), [Paragraph("p1", "abcd")]),
            new ReadingSection(new SectionId("two"), [Paragraph("p2", "ef")]));
        BookProgressIndex index = new(book);

        BookProgress progress = index.Locate(new ReadingLocation(new SectionId("two"), new BlockId("p2"), 2));

        Assert.True(progress.IsComplete);
        Assert.Equal(100m, progress.Percentage);
    }

    [Fact]
    public void SupplementarySectionsRemainPartOfFormatNeutralReadingOrderProgress()
    {
        ReadingSection primary = new(new SectionId("main"), [Paragraph("p1", "abcd")]);
        ReadingSection supplementary = new(
            new SectionId("notes"),
            [Paragraph("p2", "ef")],
            role: ReadingSectionRole.Supplementary);
        BookProgressIndex index = new(CreateBook(primary, supplementary));

        BookProgress progress = index.Locate(ReadingLocation.AtSectionStart(supplementary.Id));

        Assert.Equal(4, progress.ConsumedUnits);
        Assert.Equal(6, progress.TotalUnits);
    }

    [Fact]
    public void EmptyLogicalBookReportsZeroWithoutLayoutFallback()
    {
        Book book = CreateBook(new ReadingSection(new SectionId("empty"), [new ThematicBreakBlock(new BlockId("hr"))]));
        BookProgressIndex index = new(book);

        BookProgress progress = index.Locate(ReadingLocation.AtSectionStart(new SectionId("empty")));

        Assert.Equal(0, progress.TotalUnits);
        Assert.Equal(0m, progress.Percentage);
        Assert.False(progress.IsComplete);
    }

    [Fact]
    public void RejectsLocationOutsideIndexedBook()
    {
        BookProgressIndex index = new(CreateBook(new ReadingSection(new SectionId("one"), [Paragraph("p1", "abc")])));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => index.Locate(ReadingLocation.AtSectionStart(new SectionId("missing"))));
    }

    private static ParagraphBlock Paragraph(string id, string text) =>
        new(new BlockId(id), [new TextRun(text)]);

    private static Book CreateBook(params ReadingSection[] sections) =>
        new(new BookId("book"), new BookMetadata("Book"), sections);
}
