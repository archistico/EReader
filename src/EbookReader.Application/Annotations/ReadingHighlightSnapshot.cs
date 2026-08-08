using EbookReader.Domain.Books;

namespace EbookReader.Application.Annotations;

/// <summary>Durable highlight associated with one publication identity and path.</summary>
public sealed record ReadingHighlightSnapshot
{
    public ReadingHighlightSnapshot(string bookPath, BookId bookId, ReadingHighlightRange range)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bookPath);
        ArgumentNullException.ThrowIfNull(bookId);
        ArgumentNullException.ThrowIfNull(range);
        BookPath = Path.GetFullPath(bookPath);
        BookId = bookId;
        Range = range;
    }

    public string BookPath { get; }

    public BookId BookId { get; }

    public ReadingHighlightRange Range { get; }
}
