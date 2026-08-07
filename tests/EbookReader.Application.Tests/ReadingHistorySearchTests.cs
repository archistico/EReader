using EbookReader.Application.Library;
using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Tests;

public sealed class ReadingHistorySearchTests
{
    [Fact]
    public void EmptyQueryPreservesRecentOrder()
    {
        ReadingHistoryEntry[] entries =
        [
            CreateEntry("one.epub", "One", "Alpha"),
            CreateEntry("two.epub", "Two", "Beta"),
        ];

        var result = ReadingHistorySearch.Filter(entries, string.Empty);

        Assert.Equal(entries, result);
    }

    [Fact]
    public void SearchMatchesTitleCaseInsensitively()
    {
        ReadingHistoryEntry expected = CreateEntry("rose.epub", "Il Nome Della Rosa", "Umberto Eco");

        var result = ReadingHistorySearch.Filter([CreateEntry("other.epub", "Altro", null), expected], "ROSA");

        Assert.Equal(expected, Assert.Single(result));
    }

    [Fact]
    public void SearchMatchesAuthorAndIgnoresDiacritics()
    {
        ReadingHistoryEntry expected = CreateEntry("book.epub", "Romanzo", "José Saramago");

        var result = ReadingHistorySearch.Filter([expected], "jose");

        Assert.Equal(expected, Assert.Single(result));
    }

    [Fact]
    public void SearchMatchesFileNameAndPath()
    {
        ReadingHistoryEntry expected = CreateEntry(Path.Combine("classici", "monte-cristo.epub"), "Conte", null);

        var byFile = ReadingHistorySearch.Filter([expected], "cristo");
        var byDirectory = ReadingHistorySearch.Filter([expected], "classici");

        Assert.Equal(expected, Assert.Single(byFile));
        Assert.Equal(expected, Assert.Single(byDirectory));
    }

    [Fact]
    public void SearchSupportsFuzzySubsequence()
    {
        ReadingHistoryEntry expected = CreateEntry("rose.epub", "Il nome della rosa", "Umberto Eco");

        var result = ReadingHistorySearch.Filter([expected], "nm rosa");

        Assert.Equal(expected, Assert.Single(result));
    }

    [Fact]
    public void SearchRanksTitleMatchesBeforePathOnlyMatches()
    {
        ReadingHistoryEntry pathOnly = CreateEntry(Path.Combine("dune", "book.epub"), "Other", null);
        ReadingHistoryEntry title = CreateEntry("novel.epub", "Dune", null);

        var result = ReadingHistorySearch.Filter([pathOnly, title], "dune");

        Assert.Equal(title, result[0]);
        Assert.Equal(pathOnly, result[1]);
    }

    [Fact]
    public void SearchRequiresEveryTokenToMatch()
    {
        ReadingHistoryEntry expected = CreateEntry("book.epub", "Il nome della rosa", "Umberto Eco");
        ReadingHistoryEntry excluded = CreateEntry("other.epub", "Il nome della rosa", "Altro");

        var result = ReadingHistorySearch.Filter([excluded, expected], "rosa eco");

        Assert.Equal(expected, Assert.Single(result));
    }


    [Fact]
    public void SearchDoesNotUseFuzzySubsequenceAcrossFullPath()
    {
        ReadingHistoryEntry expected = CreateEntry("rose.epub", "Il nome della rosa", "Umberto Eco");
        ReadingHistoryEntry unrelated = CreateEntry("other.epub", "Altro", null);

        var result = ReadingHistorySearch.Filter([unrelated, expected], "rosa");

        Assert.Equal(expected, Assert.Single(result));
    }

    [Fact]
    public void SearchRejectsQueriesBeyondBound()
    {
        string query = new('a', ReadingHistorySearch.MaximumQueryLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => ReadingHistorySearch.Filter([], query));
    }

    private static ReadingHistoryEntry CreateEntry(string relativePath, string title, string? author)
    {
        string path = Path.Combine(Path.GetTempPath(), "ereader-library-search", relativePath);
        return new ReadingHistoryEntry(
            path,
            new BookId($"id-{Guid.NewGuid():N}"),
            title,
            author,
            ReadingLocation.AtSectionStart(new SectionId("one")),
            DateTimeOffset.UtcNow);
    }
}
