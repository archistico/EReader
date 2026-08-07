using EbookReader.Application.State;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Tests;

public sealed class ReadingStateTests
{
    [Fact]
    public void JsonRoundTripPreservesLogicalReadingState()
    {
        using TemporaryDirectory temporary = new();
        string statePath = Path.Combine(temporary.Path, "state.json");
        JsonReadingStateStore store = new(statePath);
        ReadingStateSnapshot expected = CreateState(
            Path.Combine(temporary.Path, "book.epub"),
            new ReadingLocation(new SectionId("chapter-2"), new BlockId("p-7"), 19));

        store.Save(expected);
        ReadingStateSnapshot? actual = store.Load();

        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MissingStateFileReturnsNull()
    {
        using TemporaryDirectory temporary = new();
        JsonReadingStateStore store = new(Path.Combine(temporary.Path, "missing", "state.json"));

        Assert.Null(store.Load());
    }

    [Fact]
    public void SaveCreatesParentDirectoryAndLeavesNoTemporaryFiles()
    {
        using TemporaryDirectory temporary = new();
        string directory = Path.Combine(temporary.Path, "nested", "EReader");
        string statePath = Path.Combine(directory, "state.json");
        JsonReadingStateStore store = new(statePath);

        store.Save(CreateState(Path.Combine(temporary.Path, "book.epub")));

        Assert.True(File.Exists(statePath));
        Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void RepeatedSaveAtomicallyReplacesPreviousSnapshot()
    {
        using TemporaryDirectory temporary = new();
        string statePath = Path.Combine(temporary.Path, "state.json");
        JsonReadingStateStore store = new(statePath);
        ReadingStateSnapshot first = CreateState(
            Path.Combine(temporary.Path, "book.epub"),
            new ReadingLocation(new SectionId("chapter-1")));
        ReadingStateSnapshot second = CreateState(
            Path.Combine(temporary.Path, "book.epub"),
            new ReadingLocation(new SectionId("chapter-2"), new BlockId("p-7"), 5));

        store.Save(first);
        store.Save(second);

        Assert.Equal(second, store.Load());
        Assert.Empty(Directory.EnumerateFiles(temporary.Path, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void PersistedJsonContainsNoLayoutCoordinates()
    {
        using TemporaryDirectory temporary = new();
        string statePath = Path.Combine(temporary.Path, "state.json");
        JsonReadingStateStore store = new(statePath);

        store.Save(CreateState(Path.Combine(temporary.Path, "book.epub")));
        string json = File.ReadAllText(statePath);

        Assert.Contains("\"schemaVersion\": 1", json, StringComparison.Ordinal);
        Assert.Contains("\"sectionId\"", json, StringComparison.Ordinal);
        Assert.Contains("\"characterOffset\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("pageNumber", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lineIndex", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("layoutPosition", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MalformedJsonIsRejected()
    {
        using TemporaryDirectory temporary = new();
        string statePath = Path.Combine(temporary.Path, "state.json");
        File.WriteAllText(statePath, "{ not-json");
        JsonReadingStateStore store = new(statePath);

        Assert.Throws<InvalidDataException>(() => store.Load());
    }

    [Fact]
    public void UnsupportedSchemaVersionIsRejected()
    {
        using TemporaryDirectory temporary = new();
        string statePath = Path.Combine(temporary.Path, "state.json");
        File.WriteAllText(
            statePath,
            """
            {
              "schemaVersion": 99,
              "lastBook": {}
            }
            """);
        JsonReadingStateStore store = new(statePath);

        Assert.Throws<InvalidDataException>(() => store.Load());
    }

    [Fact]
    public void StateLargerThanBoundIsRejectedBeforeParsing()
    {
        using TemporaryDirectory temporary = new();
        string statePath = Path.Combine(temporary.Path, "state.json");
        using (FileStream stream = new(statePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.SetLength(JsonReadingStateStore.MaximumStateBytes + 1);
        }
        JsonReadingStateStore store = new(statePath);

        Assert.Throws<InvalidDataException>(() => store.Load());
    }

    [Fact]
    public void RestoreRequiresSamePathBookIdAndValidLocation()
    {
        using TemporaryDirectory temporary = new();
        string bookPath = Path.Combine(temporary.Path, "book.epub");
        Book book = CreateBook("book-id");
        ReadingLocation location = new(new SectionId("chapter-2"), new BlockId("p-2"), 3);
        ReadingStateSnapshot state = CreateState(bookPath, location, "book-id");

        ReadingLocation? restored = ReadingStateRestore.TryGetLocation(book, bookPath, state);

        Assert.Equal(location, restored);
    }

    [Fact]
    public void RestoreRejectsDifferentPublicationIdentity()
    {
        using TemporaryDirectory temporary = new();
        string bookPath = Path.Combine(temporary.Path, "book.epub");
        Book book = CreateBook("new-book-id");
        ReadingStateSnapshot state = CreateState(bookPath, bookId: "old-book-id");

        Assert.Null(ReadingStateRestore.TryGetLocation(book, bookPath, state));
    }

    [Fact]
    public void RestoreRejectsDifferentPath()
    {
        using TemporaryDirectory temporary = new();
        Book book = CreateBook("book-id");
        ReadingStateSnapshot state = CreateState(Path.Combine(temporary.Path, "one.epub"), bookId: "book-id");

        Assert.Null(ReadingStateRestore.TryGetLocation(
            book,
            Path.Combine(temporary.Path, "two.epub"),
            state));
    }

    [Fact]
    public void RestoreRejectsLocationThatNoLongerExists()
    {
        using TemporaryDirectory temporary = new();
        string bookPath = Path.Combine(temporary.Path, "book.epub");
        Book book = CreateBook("book-id");
        ReadingStateSnapshot state = CreateState(
            bookPath,
            new ReadingLocation(new SectionId("missing")),
            "book-id");

        Assert.Null(ReadingStateRestore.TryGetLocation(book, bookPath, state));
    }

    private static ReadingStateSnapshot CreateState(
        string path,
        ReadingLocation? location = null,
        string bookId = "book-id") =>
        new(
            path,
            new BookId(bookId),
            location ?? new ReadingLocation(new SectionId("chapter-1")),
            new DateTimeOffset(2026, 8, 7, 20, 0, 0, TimeSpan.Zero));

    private static Book CreateBook(string id)
    {
        ReadingSection one = new(
            new SectionId("chapter-1"),
            [new ParagraphBlock(new BlockId("p-1"), [new TextRun("alpha")])]);
        ReadingSection two = new(
            new SectionId("chapter-2"),
            [new ParagraphBlock(new BlockId("p-2"), [new TextRun("beta gamma")])]);
        return new Book(new BookId(id), new BookMetadata("Book"), [one, two]);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"ereader-state-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
