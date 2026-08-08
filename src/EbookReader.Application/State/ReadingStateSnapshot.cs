using System.Collections.ObjectModel;
using EbookReader.Application.Annotations;
using EbookReader.Application.Library;
using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.State;

/// <summary>
/// Durable application reading state. LastBook drives global resume; Bookmarks and History retain logical positions
/// for multiple books. No layout/page/progress coordinates are persisted.
/// </summary>
public sealed record ReadingStateSnapshot
{
    public ReadingStateSnapshot(
        string bookPath,
        BookId bookId,
        ReadingLocation location,
        DateTimeOffset lastOpenedUtc,
        IEnumerable<ReadingBookmarkSnapshot>? bookmarks = null,
        IEnumerable<ReadingHistoryEntry>? history = null,
        IEnumerable<ReadingHighlightSnapshot>? highlights = null,
        IEnumerable<ReadingPersonalNoteSnapshot>? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        ArgumentNullException.ThrowIfNull(bookId);
        ArgumentNullException.ThrowIfNull(location);

        BookPath = Path.GetFullPath(bookPath);
        BookId = bookId;
        Location = location;
        LastOpenedUtc = lastOpenedUtc.ToUniversalTime();

        List<ReadingBookmarkSnapshot> bookmarkList = bookmarks?.ToList() ?? [];
        if (bookmarkList.Count > JsonReadingStateStore.MaximumBookmarks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bookmarks),
                bookmarkList.Count,
                $"Sono ammessi al massimo {JsonReadingStateStore.MaximumBookmarks} bookmark complessivi.");
        }

        Bookmarks = bookmarkList.AsReadOnly();

        List<ReadingHistoryEntry> historyList = history?.ToList() ?? [];
        if (historyList.Count > ReadingHistoryState.MaximumEntries)
        {
            throw new ArgumentOutOfRangeException(
                nameof(history),
                historyList.Count,
                $"Sono ammesse al massimo {ReadingHistoryState.MaximumEntries} voci di cronologia.");
        }

        History = historyList.AsReadOnly();

        List<ReadingHighlightSnapshot> highlightList = highlights?.ToList() ?? [];
        if (highlightList.Count > ReadingAnnotationState.MaximumHighlights)
        {
            throw new ArgumentOutOfRangeException(
                nameof(highlights),
                highlightList.Count,
                $"Sono ammesse al massimo {ReadingAnnotationState.MaximumHighlights} evidenziazioni complessive.");
        }

        List<ReadingPersonalNoteSnapshot> noteList = notes?.ToList() ?? [];
        if (noteList.Count > ReadingAnnotationState.MaximumNotes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(notes),
                noteList.Count,
                $"Sono ammesse al massimo {ReadingAnnotationState.MaximumNotes} note complessive.");
        }

        int totalNoteTextLength = noteList.Sum(item => item.Note.Text.Length);
        if (totalNoteTextLength > ReadingAnnotationState.MaximumTotalNoteTextLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(notes),
                totalNoteTextLength,
                $"Il testo complessivo delle note supera {ReadingAnnotationState.MaximumTotalNoteTextLength} code unit UTF-16.");
        }

        Highlights = highlightList.AsReadOnly();
        Notes = noteList.AsReadOnly();
    }

    public string BookPath { get; }

    public BookId BookId { get; }

    public ReadingLocation Location { get; }

    public DateTimeOffset LastOpenedUtc { get; }

    public ReadOnlyCollection<ReadingBookmarkSnapshot> Bookmarks { get; }

    public ReadOnlyCollection<ReadingHistoryEntry> History { get; }

    public ReadOnlyCollection<ReadingHighlightSnapshot> Highlights { get; }

    public ReadOnlyCollection<ReadingPersonalNoteSnapshot> Notes { get; }
}
