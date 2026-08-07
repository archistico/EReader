using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.State;

/// <summary>
/// Durable logical bookmark. The bookmark belongs to a publication identity and path and stores no layout coordinates.
/// </summary>
public sealed record ReadingBookmarkSnapshot
{
    public ReadingBookmarkSnapshot(string bookPath, BookId bookId, ReadingLocation location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        ArgumentNullException.ThrowIfNull(bookId);
        ArgumentNullException.ThrowIfNull(location);

        BookPath = Path.GetFullPath(bookPath);
        BookId = bookId;
        Location = location;
    }

    public string BookPath { get; }

    public BookId BookId { get; }

    public ReadingLocation Location { get; }
}
