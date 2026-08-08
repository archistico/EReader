using EbookReader.Application.Annotations;
using EbookReader.Application.State;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Tests;

public sealed class ReadingAnnotationTests
{
    [Fact]
    public void SchemaFourRoundTripPreservesHighlightsAndNotesWithoutLayoutCoordinates()
    {
        using TemporaryDirectory temporary = new();
        string bookPath = Path.Combine(temporary.Path, "book.epub");
        string statePath = Path.Combine(temporary.Path, "state.json");
        BookId bookId = new("annotations-book");
        ReadingHighlightSnapshot highlight = new(
            bookPath,
            bookId,
            new ReadingHighlightRange(
                new ReadingLocation(new SectionId("one"), new BlockId("p"), 2),
                new ReadingLocation(new SectionId("one"), new BlockId("p"), 8)));
        ReadingPersonalNoteSnapshot note = new(
            bookPath,
            bookId,
            new ReadingLocation(new SectionId("one"), new BlockId("p"), 4),
            "Nota personale",
            new DateTimeOffset(2026, 8, 8, 12, 0, 0, TimeSpan.Zero));
        ReadingStateSnapshot expected = new(
            bookPath,
            bookId,
            highlight.Range.Start,
            new DateTimeOffset(2026, 8, 8, 12, 0, 0, TimeSpan.Zero),
            highlights: [highlight],
            notes: [note]);
        JsonReadingStateStore store = new(statePath);

        store.Save(expected);
        ReadingStateSnapshot? actual = store.Load();
        string json = File.ReadAllText(statePath);

        Assert.NotNull(actual);
        Assert.Equal(expected.Highlights, actual.Highlights);
        Assert.Equal(expected.Notes, actual.Notes);
        Assert.Contains("\"schemaVersion\": 4", json, StringComparison.Ordinal);
        Assert.Contains("\"highlights\"", json, StringComparison.Ordinal);
        Assert.Contains("\"notes\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("pageNumber", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lineIndex", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("viewport", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SchemaThreeLoadsWithEmptyAnnotations()
    {
        using TemporaryDirectory temporary = new();
        string statePath = Path.Combine(temporary.Path, "state.json");
        string bookPath = Path.Combine(temporary.Path, "book.epub");
        File.WriteAllText(
            statePath,
            $$"""
            {
              "schemaVersion": 3,
              "lastBook": {
                "path": "{{bookPath.Replace("\\", "\\\\", StringComparison.Ordinal)}}",
                "bookId": "book-id",
                "lastOpenedUtc": "2026-08-08T12:00:00+00:00",
                "location": {
                  "sectionId": "one",
                  "blockId": "p",
                  "characterOffset": 0
                }
              },
              "bookmarks": [],
              "history": []
            }
            """);
        JsonReadingStateStore store = new(statePath);

        ReadingStateSnapshot? state = store.Load();

        Assert.NotNull(state);
        Assert.Empty(state.Highlights);
        Assert.Empty(state.Notes);
    }

    [Fact]
    public void RestoreAnnotationsRequiresPathBookIdentityAndValidLocations()
    {
        using TemporaryDirectory temporary = new();
        Book book = CreateBook("book-id");
        string bookPath = Path.Combine(temporary.Path, "book.epub");
        string otherPath = Path.Combine(temporary.Path, "other.epub");
        ReadingHighlightRange validRange = new(
            new ReadingLocation(new SectionId("one"), new BlockId("p"), 1),
            new ReadingLocation(new SectionId("one"), new BlockId("p"), 5));
        ReadingHighlightSnapshot validHighlight = new(bookPath, book.Id, validRange);
        ReadingHighlightSnapshot wrongPathHighlight = new(otherPath, book.Id, validRange);
        ReadingPersonalNoteSnapshot validNote = new(
            bookPath,
            book.Id,
            validRange.Start,
            "Valida",
            DateTimeOffset.UtcNow);
        ReadingPersonalNoteSnapshot wrongIdentityNote = new(
            bookPath,
            new BookId("other-id"),
            validRange.Start,
            "Da ignorare",
            DateTimeOffset.UtcNow);

        var highlights = ReadingAnnotationState.RestoreHighlightsForBook(
            book,
            bookPath,
            [validHighlight, wrongPathHighlight]);
        var notes = ReadingAnnotationState.RestoreNotesForBook(
            book,
            bookPath,
            [validNote, wrongIdentityNote]);

        Assert.Equal(validRange, Assert.Single(highlights));
        Assert.Equal("Valida", Assert.Single(notes).Text);
    }

    [Fact]
    public void ReplaceAnnotationsPreservesOtherBooksAndReplacesCurrentPathAsUnit()
    {
        using TemporaryDirectory temporary = new();
        Book book = CreateBook("book-id");
        string bookPath = Path.Combine(temporary.Path, "book.epub");
        string otherPath = Path.Combine(temporary.Path, "other.epub");
        ReadingHighlightRange oldRange = new(
            new ReadingLocation(new SectionId("one"), new BlockId("p"), 0),
            new ReadingLocation(new SectionId("one"), new BlockId("p"), 2));
        ReadingHighlightRange newRange = new(
            new ReadingLocation(new SectionId("one"), new BlockId("p"), 3),
            new ReadingLocation(new SectionId("one"), new BlockId("p"), 7));
        ReadingHighlightSnapshot currentOld = new(bookPath, new BookId("old-id"), oldRange);
        ReadingHighlightSnapshot other = new(otherPath, new BookId("other-id"), oldRange);
        ReadingPersonalNoteSnapshot currentOldNote = new(
            bookPath,
            new BookId("old-id"),
            oldRange.Start,
            "Vecchia",
            DateTimeOffset.UtcNow);
        ReadingPersonalNoteSnapshot otherNote = new(
            otherPath,
            new BookId("other-id"),
            oldRange.Start,
            "Altra",
            DateTimeOffset.UtcNow);
        ReadingPersonalNote newNote = new(newRange.Start, "Nuova", DateTimeOffset.UtcNow);

        var highlights = ReadingAnnotationState.ReplaceHighlightsForBook(
            book,
            bookPath,
            [currentOld, other],
            [newRange]);
        var notes = ReadingAnnotationState.ReplaceNotesForBook(
            book,
            bookPath,
            [currentOldNote, otherNote],
            [newNote]);

        Assert.Equal(2, highlights.Count);
        Assert.Contains(highlights, item => item.BookPath == Path.GetFullPath(otherPath));
        Assert.Contains(highlights, item => item.BookId == book.Id && item.Range == newRange);
        Assert.Equal(2, notes.Count);
        Assert.Contains(notes, item => item.BookPath == Path.GetFullPath(otherPath));
        Assert.Contains(notes, item => item.BookId == book.Id && item.Note.Text == "Nuova");
    }

    [Fact]
    public void HighlightRangeRejectsCrossBlockAndEmptyRanges()
    {
        ReadingLocation start = new(new SectionId("one"), new BlockId("p1"), 2);

        Assert.Throws<ArgumentException>(() => new ReadingHighlightRange(
            start,
            new ReadingLocation(new SectionId("one"), new BlockId("p2"), 4)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadingHighlightRange(
            start,
            new ReadingLocation(new SectionId("one"), new BlockId("p1"), 2)));
    }

    private static Book CreateBook(string id)
    {
        ReadingSection section = new(
            new SectionId("one"),
            [new ParagraphBlock(new BlockId("p"), [new TextRun("alpha beta gamma delta")])]);
        return new Book(new BookId(id), new BookMetadata("Annotations"), [section]);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"ereader-annotation-tests-{Guid.NewGuid():N}");
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
