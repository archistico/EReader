using System.Collections.ObjectModel;

namespace EbookReader.Application.Diagnostics;

/// <summary>
/// Immutable operation outcome plus its application-level diagnostics.
/// </summary>
public sealed class ReaderOperationSummary
{
    public ReaderOperationSummary(
        ReaderOperationStatus status,
        IEnumerable<ReaderDiagnostic>? diagnostics = null)
    {
        ReaderDiagnostic[] items = diagnostics?.ToArray() ?? [];
        Validate(status, items);

        Status = status;
        Diagnostics = new ReadOnlyCollection<ReaderDiagnostic>(items);
    }

    public ReaderOperationStatus Status { get; }

    public bool CanContinue =>
        Status is ReaderOperationStatus.Success or ReaderOperationStatus.SuccessWithDiagnostics;

    public IReadOnlyList<ReaderDiagnostic> Diagnostics { get; }

    private static void Validate(ReaderOperationStatus status, ReaderDiagnostic[] diagnostics)
    {
        bool hasFatalDocumentError = diagnostics.Any(
            diagnostic => diagnostic.Severity == ReaderDiagnosticSeverity.FatalDocumentError);
        bool hasInternalError = diagnostics.Any(
            diagnostic => diagnostic.Severity == ReaderDiagnosticSeverity.InternalError);

        switch (status)
        {
            case ReaderOperationStatus.Success:
                if (diagnostics.Length != 0)
                {
                    throw new ArgumentException(
                        "Success non può contenere diagnostiche; usare SuccessWithDiagnostics.",
                        nameof(diagnostics));
                }

                break;

            case ReaderOperationStatus.SuccessWithDiagnostics:
                if (diagnostics.Length == 0 || hasFatalDocumentError || hasInternalError)
                {
                    throw new ArgumentException(
                        "SuccessWithDiagnostics richiede diagnostiche non fatali e non interne.",
                        nameof(diagnostics));
                }

                break;

            case ReaderOperationStatus.DocumentUnreadable:
                if (!hasFatalDocumentError || hasInternalError)
                {
                    throw new ArgumentException(
                        "DocumentUnreadable richiede almeno un FatalDocumentError e nessun InternalError.",
                        nameof(diagnostics));
                }

                break;

            case ReaderOperationStatus.InternalFailure:
                if (!hasInternalError)
                {
                    throw new ArgumentException(
                        "InternalFailure richiede almeno un InternalError.",
                        nameof(diagnostics));
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Stato operazione non supportato.");
        }
    }
}
