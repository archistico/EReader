using System.Collections.ObjectModel;
using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.State;

/// <summary>
/// Restores and replaces bookmark sets for one publication while preserving bookmark sets for other books.
/// </summary>
public static class ReadingBookmarkState
{
    public const int MaximumBookmarksPerBook = 1_000;

    public static ReadOnlyCollection<ReadingLocation> RestoreForBook(
        Book book,
        string currentBookPath,
        ReadingStateSnapshot? state)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentBookPath);

        if (state is null || state.Bookmarks.Count == 0)
        {
            return new List<ReadingLocation>().AsReadOnly();
        }

        string fullPath = Path.GetFullPath(currentBookPath);
        List<ReadingLocation> locations = [];

        foreach (ReadingBookmarkSnapshot bookmark in state.Bookmarks)
        {
            if (!PathsEqual(fullPath, bookmark.BookPath)
                || bookmark.BookId != book.Id
                || !book.ContainsLocation(bookmark.Location)
                || locations.Contains(bookmark.Location))
            {
                continue;
            }

            locations.Add(bookmark.Location);
        }

        return locations.AsReadOnly();
    }

    public static ReadOnlyCollection<ReadingBookmarkSnapshot> ReplaceForBook(
        Book book,
        string currentBookPath,
        IEnumerable<ReadingBookmarkSnapshot>? existingBookmarks,
        IEnumerable<ReadingLocation> currentBookLocations)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentBookPath);
        ArgumentNullException.ThrowIfNull(currentBookLocations);

        string fullPath = Path.GetFullPath(currentBookPath);
        ReadingLocation[] locations = currentBookLocations.Distinct().ToArray();
        if (locations.Length > MaximumBookmarksPerBook)
        {
            throw new InvalidOperationException(
                $"Un libro può contenere al massimo {MaximumBookmarksPerBook} bookmark.");
        }

        List<ReadingBookmarkSnapshot> merged = [];
        if (existingBookmarks is not null)
        {
            foreach (ReadingBookmarkSnapshot bookmark in existingBookmarks)
            {
                // Same path is replaced as a unit. This also removes stale bookmarks when the EPUB at that path changed identity.
                if (!PathsEqual(fullPath, bookmark.BookPath))
                {
                    merged.Add(bookmark);
                }
            }
        }

        foreach (ReadingLocation location in locations)
        {
            if (!book.ContainsLocation(location))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentBookLocations),
                    location,
                    "Un bookmark da salvare non appartiene al libro corrente.");
            }

            merged.Add(new ReadingBookmarkSnapshot(fullPath, book.Id, location));
        }

        if (merged.Count > JsonReadingStateStore.MaximumBookmarks)
        {
            throw new InvalidOperationException(
                $"Lo stato può contenere al massimo {JsonReadingStateStore.MaximumBookmarks} bookmark complessivi.");
        }

        return merged.AsReadOnly();
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
