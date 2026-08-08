using System.Collections.ObjectModel;
using EbookReader.Domain.Books;

namespace EbookReader.Application.Annotations;

/// <summary>
/// Restores/replaces annotations for one publication while preserving annotations belonging to other books.
/// Same-path replacement also removes stale annotations when publication identity changes.
/// </summary>
public static class ReadingAnnotationState
{
    public const int MaximumHighlights = 1_000;
    public const int MaximumHighlightsPerBook = 250;
    public const int MaximumNotes = 500;
    public const int MaximumNotesPerBook = 100;
    public const int MaximumNoteTextLength = 2_048;
    public const int MaximumTotalNoteTextLength = 131_072;

    public static ReadOnlyCollection<ReadingHighlightRange> RestoreHighlightsForBook(
        Book book,
        string currentBookPath,
        IEnumerable<ReadingHighlightSnapshot>? highlights)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentBookPath);
        if (highlights is null)
        {
            return Array.AsReadOnly(Array.Empty<ReadingHighlightRange>());
        }

        string fullPath = Path.GetFullPath(currentBookPath);
        List<ReadingHighlightRange> result = [];
        foreach (ReadingHighlightSnapshot snapshot in highlights)
        {
            if (!PathsEqual(fullPath, snapshot.BookPath)
                || snapshot.BookId != book.Id
                || !book.ContainsLocation(snapshot.Range.Start)
                || !book.ContainsLocation(snapshot.Range.End)
                || result.Contains(snapshot.Range))
            {
                continue;
            }

            result.Add(snapshot.Range);
        }

        return result.AsReadOnly();
    }

    public static ReadOnlyCollection<ReadingPersonalNote> RestoreNotesForBook(
        Book book,
        string currentBookPath,
        IEnumerable<ReadingPersonalNoteSnapshot>? notes)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentBookPath);
        if (notes is null)
        {
            return Array.AsReadOnly(Array.Empty<ReadingPersonalNote>());
        }

        string fullPath = Path.GetFullPath(currentBookPath);
        List<ReadingPersonalNote> result = [];
        foreach (ReadingPersonalNoteSnapshot snapshot in notes)
        {
            ReadingPersonalNote note = snapshot.Note;
            if (!PathsEqual(fullPath, snapshot.BookPath)
                || snapshot.BookId != book.Id
                || !book.ContainsLocation(note.Location)
                || result.Any(existing => existing.Location == note.Location))
            {
                continue;
            }

            result.Add(note);
        }

        return result.AsReadOnly();
    }

    public static ReadOnlyCollection<ReadingHighlightSnapshot> ReplaceHighlightsForBook(
        Book book,
        string currentBookPath,
        IEnumerable<ReadingHighlightSnapshot>? existing,
        IEnumerable<ReadingHighlightRange> currentBookHighlights)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentBookPath);
        ArgumentNullException.ThrowIfNull(currentBookHighlights);

        string fullPath = Path.GetFullPath(currentBookPath);
        ReadingHighlightRange[] ranges = currentBookHighlights.Distinct().ToArray();
        if (ranges.Length > MaximumHighlightsPerBook)
        {
            throw new InvalidOperationException($"Un libro può contenere al massimo {MaximumHighlightsPerBook} evidenziazioni.");
        }

        List<ReadingHighlightSnapshot> merged = existing is null
            ? []
            : existing.Where(item => !PathsEqual(fullPath, item.BookPath)).ToList();
        foreach (ReadingHighlightRange range in ranges)
        {
            if (!book.ContainsLocation(range.Start) || !book.ContainsLocation(range.End))
            {
                throw new ArgumentOutOfRangeException(nameof(currentBookHighlights), range, "Range di evidenziazione non appartenente al libro corrente.");
            }

            merged.Add(new ReadingHighlightSnapshot(fullPath, book.Id, range));
        }

        if (merged.Count > MaximumHighlights)
        {
            throw new InvalidOperationException($"Lo stato può contenere al massimo {MaximumHighlights} evidenziazioni complessive.");
        }

        return merged.AsReadOnly();
    }

    public static ReadOnlyCollection<ReadingPersonalNoteSnapshot> ReplaceNotesForBook(
        Book book,
        string currentBookPath,
        IEnumerable<ReadingPersonalNoteSnapshot>? existing,
        IEnumerable<ReadingPersonalNote> currentBookNotes)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentBookPath);
        ArgumentNullException.ThrowIfNull(currentBookNotes);

        string fullPath = Path.GetFullPath(currentBookPath);
        ReadingPersonalNote[] notes = currentBookNotes
            .GroupBy(note => note.Location)
            .Select(group => group.OrderByDescending(note => note.UpdatedUtc).First())
            .ToArray();
        if (notes.Length > MaximumNotesPerBook)
        {
            throw new InvalidOperationException($"Un libro può contenere al massimo {MaximumNotesPerBook} note personali.");
        }

        List<ReadingPersonalNoteSnapshot> merged = existing is null
            ? []
            : existing.Where(item => !PathsEqual(fullPath, item.BookPath)).ToList();
        foreach (ReadingPersonalNote note in notes)
        {
            if (!book.ContainsLocation(note.Location))
            {
                throw new ArgumentOutOfRangeException(nameof(currentBookNotes), note, "Nota non appartenente al libro corrente.");
            }

            merged.Add(new ReadingPersonalNoteSnapshot(fullPath, book.Id, note.Location, note.Text, note.UpdatedUtc));
        }

        if (merged.Count > MaximumNotes)
        {
            throw new InvalidOperationException($"Lo stato può contenere al massimo {MaximumNotes} note complessive.");
        }

        int totalTextLength = merged.Sum(item => item.Note.Text.Length);
        if (totalTextLength > MaximumTotalNoteTextLength)
        {
            throw new InvalidOperationException(
                $"Il testo complessivo delle note supera {MaximumTotalNoteTextLength} code unit UTF-16.");
        }

        return merged.AsReadOnly();
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
