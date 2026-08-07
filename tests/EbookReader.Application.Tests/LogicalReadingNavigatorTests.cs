using EbookReader.Application.Reading;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Tests;

public sealed class LogicalReadingNavigatorTests
{
    [Fact]
    public void ChapterStartReturnsSectionStart()
    {
        ReadingSection section = Section("one", ReadingSectionRole.Primary, "alpha");
        Book book = CreateBook(section);
        ReadingLocation current = new(section.Id, section.Blocks[0].Id, 3);

        Assert.Equal(ReadingLocation.AtSectionStart(section.Id), LogicalReadingNavigator.ChapterStart(book, current));
    }

    [Fact]
    public void ChapterEndReturnsEndOfLastBlock()
    {
        ReadingSection section = new(
            new SectionId("one"),
            [
                new ParagraphBlock(new BlockId("a"), [new TextRun("alpha")]),
                new ParagraphBlock(new BlockId("b"), [new TextRun("beta")]),
            ]);
        Book book = CreateBook(section);

        ReadingLocation end = LogicalReadingNavigator.ChapterEnd(book, ReadingLocation.AtSectionStart(section.Id));

        Assert.Equal(new ReadingLocation(section.Id, new BlockId("b"), 4), end);
    }

    [Fact]
    public void EmptyChapterEndEqualsSectionStart()
    {
        ReadingSection empty = new(new SectionId("empty"), []);
        ReadingSection nonEmpty = Section("two", ReadingSectionRole.Primary, "content");
        Book book = CreateBook(empty, nonEmpty);

        ReadingLocation end = LogicalReadingNavigator.ChapterEnd(book, ReadingLocation.AtSectionStart(empty.Id));

        Assert.Equal(ReadingLocation.AtSectionStart(empty.Id), end);
    }

    [Fact]
    public void NextAndPreviousChapterSkipSupplementarySections()
    {
        ReadingSection one = Section("one", ReadingSectionRole.Primary, "one");
        ReadingSection notes = Section("notes", ReadingSectionRole.Supplementary, "notes");
        ReadingSection two = Section("two", ReadingSectionRole.Primary, "two");
        Book book = CreateBook(one, notes, two);

        ReadingLocation? next = LogicalReadingNavigator.NextChapter(book, ReadingLocation.AtSectionStart(one.Id));
        ReadingLocation? previous = LogicalReadingNavigator.PreviousChapter(book, ReadingLocation.AtSectionStart(two.Id));

        Assert.Equal(ReadingLocation.AtSectionStart(two.Id), next);
        Assert.Equal(ReadingLocation.AtSectionStart(one.Id), previous);
    }

    [Fact]
    public void ChapterNavigationReturnsNullAtPrimaryBoundaries()
    {
        ReadingSection one = Section("one", ReadingSectionRole.Primary, "one");
        ReadingSection two = Section("two", ReadingSectionRole.Primary, "two");
        Book book = CreateBook(one, two);

        Assert.Null(LogicalReadingNavigator.PreviousChapter(book, ReadingLocation.AtSectionStart(one.Id)));
        Assert.Null(LogicalReadingNavigator.NextChapter(book, ReadingLocation.AtSectionStart(two.Id)));
    }

    [Fact]
    public void LogicalNavigationRejectsForeignLocation()
    {
        ReadingSection section = Section("one", ReadingSectionRole.Primary, "one");
        Book book = CreateBook(section);
        ReadingLocation foreign = ReadingLocation.AtSectionStart(new SectionId("missing"));

        Assert.Throws<ArgumentOutOfRangeException>(() => LogicalReadingNavigator.ChapterStart(book, foreign));
    }

    private static ReadingSection Section(string id, ReadingSectionRole role, string text) =>
        new(
            new SectionId(id),
            [new ParagraphBlock(new BlockId($"{id}-p"), [new TextRun(text)])],
            role: role);

    private static Book CreateBook(params ReadingSection[] sections) =>
        new(new BookId("book"), new BookMetadata("Book"), sections);
}
