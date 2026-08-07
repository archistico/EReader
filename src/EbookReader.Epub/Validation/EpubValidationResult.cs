using System.Collections.ObjectModel;
using EbookReader.Domain.Books;

namespace EbookReader.Epub.Validation;

/// <summary>
/// Non-throwing result of the supported EPUB ingestion pipeline.
/// </summary>
public sealed class EpubValidationResult
{
    internal EpubValidationResult(
        EpubValidationStatus status,
        Book? book,
        EpubProtectionReport? protection,
        List<EpubDiagnostic> diagnostics)
    {
        if (status == EpubValidationStatus.Valid && book is null)
        {
            throw new ArgumentException("Un risultato EPUB valido deve contenere il Book Domain.", nameof(book));
        }

        if (status != EpubValidationStatus.Valid && book is not null)
        {
            throw new ArgumentException("Un risultato EPUB non valido non può esporre un Book Domain.", nameof(book));
        }

        Status = status;
        Book = book;
        Protection = protection;
        Diagnostics = new ReadOnlyCollection<EpubDiagnostic>(diagnostics);
    }

    public EpubValidationStatus Status { get; }

    public bool CanRead => Status == EpubValidationStatus.Valid;

    public Book? Book { get; }

    public EpubProtectionReport? Protection { get; }

    public IReadOnlyList<EpubDiagnostic> Diagnostics { get; }
}
