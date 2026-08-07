using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Library;

public sealed record ReadingHistoryEntry
{
    public ReadingHistoryEntry(
        string bookPath,
        BookId bookId,
        string title,
        string? authorLine,
        ReadingLocation location,
        DateTimeOffset lastOpenedUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        ArgumentNullException.ThrowIfNull(bookId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(location);

        BookPath = Path.GetFullPath(bookPath);
        BookId = bookId;
        Title = title.Trim();
        AuthorLine = string.IsNullOrWhiteSpace(authorLine) ? null : authorLine.Trim();
        Location = location;
        LastOpenedUtc = lastOpenedUtc.ToUniversalTime();
    }

    public string BookPath { get; }
    public BookId BookId { get; }
    public string Title { get; }
    public string? AuthorLine { get; }
    public ReadingLocation Location { get; }
    public DateTimeOffset LastOpenedUtc { get; }
}
