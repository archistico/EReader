using System.Collections.ObjectModel;
using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.State;

/// <summary>
/// Durable application reading state. LastBook drives resume; Bookmarks can retain logical positions for multiple books.
/// No layout/page coordinates are persisted.
/// </summary>
public sealed record ReadingStateSnapshot
{
    public ReadingStateSnapshot(
        string bookPath,
        BookId bookId,
        ReadingLocation location,
        DateTimeOffset lastOpenedUtc,
        IEnumerable<ReadingBookmarkSnapshot>? bookmarks = null)
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
    }

    public string BookPath { get; }

    public BookId BookId { get; }

    public ReadingLocation Location { get; }

    public DateTimeOffset LastOpenedUtc { get; }

    public ReadOnlyCollection<ReadingBookmarkSnapshot> Bookmarks { get; }
}
