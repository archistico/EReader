using EbookReader.Application.Library;
using EbookReader.Application.State;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Tests;

public sealed class ReadingHistoryTests
{
    [Fact]
    public void UpdatePlacesCurrentBookFirstAndReplacesSamePath()
    {
        using TemporaryDirectory temporary = new();
        string currentPath = Path.Combine(temporary.Path, "current.epub");
        string otherPath = Path.Combine(temporary.Path, "other.epub");
        Book current = CreateBook("new-id", "Current");
        ReadingHistoryEntry stale = new(currentPath, new BookId("old-id"), "Old", null,
            ReadingLocation.AtSectionStart(new SectionId("one")), DateTimeOffset.UtcNow.AddDays(-2));
        ReadingHistoryEntry other = new(otherPath, new BookId("other-id"), "Other", null,
            ReadingLocation.AtSectionStart(new SectionId("one")), DateTimeOffset.UtcNow.AddDays(-1));
        DateTimeOffset opened = new(2026, 8, 8, 0, 30, 0, TimeSpan.Zero);

        var history = ReadingHistoryState.Update(
            current,
            currentPath,
            [other, stale],
            new ReadingLocation(new SectionId("one"), new BlockId("p"), 2),
            opened);

        Assert.Equal(2, history.Count);
        Assert.Equal(Path.GetFullPath(currentPath), history[0].BookPath);
        Assert.Equal(current.Id, history[0].BookId);
        Assert.Equal("Current", history[0].Title);
        Assert.DoesNotContain(history, item => item.BookId == new BookId("old-id"));
    }

    [Fact]
    public void UpdateCapturesAuthorMetadataWithoutDependingOnEpub()
    {
        using TemporaryDirectory temporary = new();
        Book book = CreateBook("id", "Titolo", "Emilie Rollandin");

        var history = ReadingHistoryState.Update(
            book,
            Path.Combine(temporary.Path, "book.epub"),
            null,
            ReadingLocation.AtSectionStart(new SectionId("one")),
            DateTimeOffset.UtcNow);

        Assert.Equal("Emilie Rollandin", Assert.Single(history).AuthorLine);
    }

    [Fact]
    public void FindForBookReturnsMatchingPathAndIdentity()
    {
        using TemporaryDirectory temporary = new();
        string path = Path.Combine(temporary.Path, "book.epub");
        Book book = CreateBook("id", "Book");
        ReadingHistoryEntry other = new(
            Path.Combine(temporary.Path, "other.epub"),
            book.Id,
            "Other",
            null,
            ReadingLocation.AtSectionStart(new SectionId("one")),
            DateTimeOffset.UtcNow);
        ReadingHistoryEntry expected = new(
            path,
            book.Id,
            "Book",
            null,
            ReadingLocation.AtSectionStart(new SectionId("one")),
            DateTimeOffset.UtcNow);

        ReadingHistoryEntry? found = ReadingHistoryState.FindForBook(book, path, [other, expected]);

        Assert.Equal(expected, found);
    }

    [Fact]
    public void UpdateCapsHistoryAtTwoHundredMostRecentBooks()
    {
        using TemporaryDirectory temporary = new();
        ReadingLocation location = ReadingLocation.AtSectionStart(new SectionId("one"));
        DateTimeOffset origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        List<ReadingHistoryEntry> existing = [];
        for (int index = 0; index < ReadingHistoryState.MaximumEntries; index++)
        {
            existing.Add(new ReadingHistoryEntry(
                Path.Combine(temporary.Path, $"book-{index}.epub"),
                new BookId($"id-{index}"),
                $"Book {index}",
                null,
                location,
                origin.AddMinutes(index)));
        }
        Book newest = CreateBook("newest", "Newest");

        var history = ReadingHistoryState.Update(
            newest,
            Path.Combine(temporary.Path, "newest.epub"),
            existing,
            location,
            origin.AddYears(1));

        Assert.Equal(ReadingHistoryState.MaximumEntries, history.Count);
        Assert.Equal(newest.Id, history[0].BookId);
        Assert.DoesNotContain(history, item => item.BookId == new BookId("id-0"));
    }

    [Fact]
    public void TryGetLocationRequiresPathBookIdentityAndValidLocation()
    {
        using TemporaryDirectory temporary = new();
        string path = Path.Combine(temporary.Path, "book.epub");
        Book book = CreateBook("id", "Book");
        ReadingLocation location = new(new SectionId("one"), new BlockId("p"), 2);
        ReadingHistoryEntry valid = new(path, book.Id, "Book", null, location, DateTimeOffset.UtcNow);

        Assert.Equal(location, ReadingHistoryState.TryGetLocation(book, path, valid));
        Assert.Null(ReadingHistoryState.TryGetLocation(book, path,
            new ReadingHistoryEntry(path, new BookId("other"), "Book", null, location, DateTimeOffset.UtcNow)));
        Assert.Null(ReadingHistoryState.TryGetLocation(book, path,
            new ReadingHistoryEntry(path, book.Id, "Book", null, new ReadingLocation(new SectionId("missing")), DateTimeOffset.UtcNow)));
    }

    [Fact]
    public void SchemaThreeRoundTripPreservesHistoryWithoutLayoutCoordinates()
    {
        using TemporaryDirectory temporary = new();
        string path = Path.Combine(temporary.Path, "book.epub");
        ReadingLocation location = new(new SectionId("one"), new BlockId("p"), 2);
        ReadingHistoryEntry item = new(path, new BookId("id"), "Titolo", "Autore", location,
            new DateTimeOffset(2026, 8, 8, 0, 30, 0, TimeSpan.Zero));
        ReadingStateSnapshot state = new(path, item.BookId, location, item.LastOpenedUtc, history: [item]);
        string statePath = Path.Combine(temporary.Path, "state.json");
        JsonReadingStateStore store = new(statePath);

        store.Save(state);
        ReadingStateSnapshot? loaded = store.Load();
        string json = File.ReadAllText(statePath);

        Assert.NotNull(loaded);
        Assert.Equal(item, Assert.Single(loaded.History));
        Assert.Contains("\"schemaVersion\": 4", json, StringComparison.Ordinal);
        Assert.Contains("\"history\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("pageNumber", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lineIndex", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("progress", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SchemaTwoPromotesLastBookIntoHistory()
    {
        using TemporaryDirectory temporary = new();
        string bookPath = Path.Combine(temporary.Path, "legacy.epub");
        string statePath = Path.Combine(temporary.Path, "state.json");
        File.WriteAllText(statePath,
            $$"""
            {
              "schemaVersion": 2,
              "lastBook": {
                "path": "{{bookPath.Replace("\\", "\\\\", StringComparison.Ordinal)}}",
                "bookId": "legacy-id",
                "lastOpenedUtc": "2026-08-07T20:00:00+00:00",
                "location": { "sectionId": "one", "characterOffset": 0 }
              },
              "bookmarks": []
            }
            """);

        ReadingStateSnapshot? state = new JsonReadingStateStore(statePath).Load();

        Assert.NotNull(state);
        ReadingHistoryEntry history = Assert.Single(state.History);
        Assert.Equal(Path.GetFullPath(bookPath), history.BookPath);
        Assert.Equal("legacy", history.Title);
    }

    private static Book CreateBook(string id, string title, string? author = null)
    {
        BookContributor[] contributors = author is null ? [] : [new BookContributor(author, ContributorRole.Author)];
        ReadingSection section = new(new SectionId("one"), [new ParagraphBlock(new BlockId("p"), [new TextRun("alpha")])]);
        return new Book(new BookId(id), new BookMetadata(title, contributors: contributors), [section]);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ereader-history-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }
}
