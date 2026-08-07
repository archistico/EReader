using System.Collections.ObjectModel;
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
        IEnumerable<ReadingHistoryEntry>? history = null)
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
    }

    public string BookPath { get; }

    public BookId BookId { get; }

    public ReadingLocation Location { get; }

    public DateTimeOffset LastOpenedUtc { get; }

    public ReadOnlyCollection<ReadingBookmarkSnapshot> Bookmarks { get; }

    public ReadOnlyCollection<ReadingHistoryEntry> History { get; }
}
