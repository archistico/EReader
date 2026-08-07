using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.State;

/// <summary>
/// Durable reading state for the single most recently opened book.
/// It deliberately stores only logical Domain coordinates, never layout/page coordinates.
/// </summary>
public sealed record ReadingStateSnapshot
{
    public ReadingStateSnapshot(
        string bookPath,
        BookId bookId,
        ReadingLocation location,
        DateTimeOffset lastOpenedUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        ArgumentNullException.ThrowIfNull(bookId);
        ArgumentNullException.ThrowIfNull(location);

        BookPath = Path.GetFullPath(bookPath);
        BookId = bookId;
        Location = location;
        LastOpenedUtc = lastOpenedUtc.ToUniversalTime();
    }

    public string BookPath { get; }

    public BookId BookId { get; }

    public ReadingLocation Location { get; }

    public DateTimeOffset LastOpenedUtc { get; }
}
