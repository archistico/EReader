namespace EbookReader.Application.Annotations;

/// <summary>
/// Annotation capacity available to the current book after retaining other books' annotations.
/// Same-path annotations are replaced on commit and therefore do not consume this budget.
/// </summary>
public sealed class ReadingAnnotationBudget
{
    private ReadingAnnotationBudget(int highlights, int notes, int noteTextLength)
    {
        MaximumHighlights = highlights;
        MaximumNotes = notes;
        MaximumNoteTextLength = noteTextLength;
    }

    public static ReadingAnnotationBudget Default { get; } = new(
        ReadingAnnotationState.MaximumHighlightsPerBook,
        ReadingAnnotationState.MaximumNotesPerBook,
        ReadingAnnotationState.MaximumTotalNoteTextLength);

    public int MaximumHighlights { get; }

    public int MaximumNotes { get; }

    public int MaximumNoteTextLength { get; }

    public static ReadingAnnotationBudget ForBook(
        string bookPath,
        IEnumerable<ReadingHighlightSnapshot>? highlights,
        IEnumerable<ReadingPersonalNoteSnapshot>? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        string fullPath = Path.GetFullPath(bookPath);
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        bool BelongsToOtherBook(string path) =>
            !string.Equals(fullPath, Path.GetFullPath(path), comparison);

        int retainedHighlights = highlights?.Count(item => BelongsToOtherBook(item.BookPath)) ?? 0;
        ReadingPersonalNoteSnapshot[] retainedNotes = notes?
            .Where(item => BelongsToOtherBook(item.BookPath)).ToArray() ?? [];

        return new ReadingAnnotationBudget(
            Math.Clamp(ReadingAnnotationState.MaximumHighlights - retainedHighlights,
                0, ReadingAnnotationState.MaximumHighlightsPerBook),
            Math.Clamp(ReadingAnnotationState.MaximumNotes - retainedNotes.Length,
                0, ReadingAnnotationState.MaximumNotesPerBook),
            Math.Max(0, ReadingAnnotationState.MaximumTotalNoteTextLength
                - retainedNotes.Sum(item => item.Note.Text.Length)));
    }
}
