using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Annotations;

/// <summary>Durable personal note associated with one publication identity and path.</summary>
public sealed record ReadingPersonalNoteSnapshot
{
    public ReadingPersonalNoteSnapshot(
        string bookPath,
        BookId bookId,
        ReadingLocation location,
        string text,
        DateTimeOffset updatedUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        ArgumentNullException.ThrowIfNull(bookId);
        Note = new ReadingPersonalNote(location, text, updatedUtc);
        BookPath = Path.GetFullPath(bookPath);
        BookId = bookId;
    }

    public string BookPath { get; }

    public BookId BookId { get; }

    public ReadingPersonalNote Note { get; }
}
