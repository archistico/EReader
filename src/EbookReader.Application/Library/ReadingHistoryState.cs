using System.Collections.ObjectModel;
using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Library;

public static class ReadingHistoryState
{
    public const int MaximumEntries = 200;

    public static ReadOnlyCollection<ReadingHistoryEntry> Update(
        Book book,
        string bookPath,
        IEnumerable<ReadingHistoryEntry>? existing,
        ReadingLocation location,
        DateTimeOffset openedUtc)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        ArgumentNullException.ThrowIfNull(location);

        string fullPath = Path.GetFullPath(bookPath);
        List<ReadingHistoryEntry> entries = existing?.ToList() ?? [];
        entries.RemoveAll(item => PathsEqual(item.BookPath, fullPath));
        entries.Add(new ReadingHistoryEntry(
            fullPath,
            book.Id,
            book.Metadata.Title,
            BuildAuthorLine(book),
            location,
            openedUtc));

        ReadingHistoryEntry[] ordered = entries
            .OrderByDescending(item => item.LastOpenedUtc)
            .Take(MaximumEntries)
            .ToArray();
        return Array.AsReadOnly(ordered);
    }


    public static ReadingHistoryEntry? FindForBook(
        Book book,
        string bookPath,
        IEnumerable<ReadingHistoryEntry>? history)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        if (history is null)
        {
            return null;
        }

        string fullPath = Path.GetFullPath(bookPath);
        return history.FirstOrDefault(item => PathsEqual(item.BookPath, fullPath) && item.BookId == book.Id);
    }

    public static ReadingLocation? TryGetLocation(Book book, string bookPath, ReadingHistoryEntry? entry)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        if (entry is null || !PathsEqual(entry.BookPath, Path.GetFullPath(bookPath)) || entry.BookId != book.Id)
        {
            return null;
        }

        return book.ContainsLocation(entry.Location) ? entry.Location : null;
    }

    private static string? BuildAuthorLine(Book book)
    {
        string[] authors = book.Metadata.Contributors
            .Where(contributor => contributor.Role == ContributorRole.Author)
            .Select(contributor => contributor.Name)
            .ToArray();
        return authors.Length == 0 ? null : string.Join(", ", authors);
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(left, right, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
