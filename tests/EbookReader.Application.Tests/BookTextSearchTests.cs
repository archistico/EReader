using EbookReader.Application.Search;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Tests;

public sealed class BookTextSearchTests
{
    private static readonly int[] ExpectedOverlappingOffsets = [1, 3];

    [Fact]
    public void SearchIsCaseInsensitiveAndReturnsLogicalUtf16Offsets()
    {
        ReadingSection section = new(
            new SectionId("one"),
            [new ParagraphBlock(new BlockId("p"), [new TextRun("Alpha 😀 BETA alpha")])]);
        Book book = CreateBook(section);

        BookSearchResultSet results = BookTextSearch.Search(book, "alpha");

        Assert.Equal("alpha", results.Query);
        Assert.False(results.IsTruncated);
        Assert.Collection(
            results.Matches,
            match => Assert.Equal(new ReadingLocation(section.Id, new BlockId("p"), 0), match.Location),
            match => Assert.Equal(new ReadingLocation(section.Id, new BlockId("p"), 14), match.Location));
        Assert.All(results.Matches, match => Assert.Equal(5, match.MatchLength));
    }

    [Fact]
    public void SearchCrossesInlineFormattingBoundariesBeforeLayout()
    {
        ReadingSection section = new(
            new SectionId("one"),
            [
                new ParagraphBlock(
                    new BlockId("p"),
                    [
                        new TextRun("golden "),
                        new StrongSpan([new TextRun("key")]),
                        new TextRun(" opens"),
                    ]),
            ]);
        Book book = CreateBook(section);

        BookSearchResultSet results = BookTextSearch.Search(book, "golden key");

        BookSearchMatch match = Assert.Single(results.Matches);
        Assert.Equal(new ReadingLocation(section.Id, new BlockId("p"), 0), match.Location);
        Assert.Equal(10, match.MatchLength);
    }

    [Fact]
    public void SearchFindsOverlappingMatches()
    {
        ReadingSection section = new(
            new SectionId("one"),
            [new ParagraphBlock(new BlockId("p"), [new TextRun("banana")])]);
        Book book = CreateBook(section);

        BookSearchResultSet results = BookTextSearch.Search(book, "ana");

        Assert.Equal(ExpectedOverlappingOffsets, results.Matches.Select(match => match.Location.CharacterOffset).ToArray());
    }

    [Fact]
    public void SearchIncludesSupplementaryReadingSectionsInReadingOrder()
    {
        ReadingSection primary = new(
            new SectionId("main"),
            [new ParagraphBlock(new BlockId("p1"), [new TextRun("main text")])]);
        ReadingSection notes = new(
            new SectionId("notes"),
            [new ParagraphBlock(new BlockId("p2"), [new TextRun("needle in notes")])],
            role: ReadingSectionRole.Supplementary);
        Book book = CreateBook(primary, notes);

        BookSearchResultSet results = BookTextSearch.Search(book, "needle");

        BookSearchMatch match = Assert.Single(results.Matches);
        Assert.Equal(notes.Id, match.Location.SectionId);
    }

    [Fact]
    public void SearchResultsAreBoundedAndReportTruncation()
    {
        string text = new('a', BookTextSearch.MaximumMatches + 50);
        ReadingSection section = new(
            new SectionId("one"),
            [new ParagraphBlock(new BlockId("p"), [new TextRun(text)])]);
        Book book = CreateBook(section);

        BookSearchResultSet results = BookTextSearch.Search(book, "a");

        Assert.True(results.IsTruncated);
        Assert.Equal(BookTextSearch.MaximumMatches, results.Matches.Count);
    }

    [Fact]
    public void SearchRejectsBlankOrOversizedQueries()
    {
        Book book = CreateBook(new ReadingSection(new SectionId("one"), []));

        Assert.Throws<ArgumentException>(() => BookTextSearch.Search(book, "   "));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BookTextSearch.Search(book, new string('x', BookTextSearch.MaximumQueryLength + 1)));
    }

    [Fact]
    public void SearchUsesImageLogicalTextWithoutKnowingRendering()
    {
        ReadingSection section = new(
            new SectionId("one"),
            [new ImageBlock(new BlockId("img"), new EbookReader.Domain.Resources.ResourceId("cover"), "Mappa antica", "Pianta del monastero")]);
        Book book = new(
            new BookId("book"),
            new BookMetadata("Book"),
            [section],
            resources:
            [
                new EbookReader.Domain.Resources.BookResource(
                    new EbookReader.Domain.Resources.ResourceId("cover"),
                    EbookReader.Domain.Resources.ResourceKind.Image,
                    "image/jpeg"),
            ]);

        BookSearchResultSet results = BookTextSearch.Search(book, "Pianta");

        BookSearchMatch match = Assert.Single(results.Matches);
        Assert.Equal(13, match.Location.CharacterOffset);
    }

    private static Book CreateBook(params ReadingSection[] sections) =>
        new(new BookId("book"), new BookMetadata("Book"), sections);
}
