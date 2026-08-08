using System.Collections.ObjectModel;
using EbookReader.Domain.Books;

namespace EbookReader.Epub.Content;

internal enum EpubContentRecoveryKind
{
    SupplementarySpineItemSkipped = 0,
    TableOfContentsDropped = 1,
}

internal sealed class EpubContentRecoveryIssue
{
    public EpubContentRecoveryIssue(EpubContentRecoveryKind kind, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Kind = kind;
        Message = message;
    }

    public EpubContentRecoveryKind Kind { get; }

    public string Message { get; }
}

internal sealed class EpubBookRecoveryResult
{
    public EpubBookRecoveryResult(Book book, EpubContentRecoveryIssue[] issues)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(issues);
        Book = book;
        Issues = Array.AsReadOnly((EpubContentRecoveryIssue[])issues.Clone());
    }

    public Book Book { get; }

    public ReadOnlyCollection<EpubContentRecoveryIssue> Issues { get; }
}
