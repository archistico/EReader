using EbookReader.Application.Diagnostics;

namespace EbookReader.Cli.Diagnostics;

/// <summary>
/// Composition-root mapping for unexpected reader/runtime failures.
/// These failures are deliberately not classified as malformed EPUB input.
/// </summary>
internal static class ReaderInternalFailureBridge
{
    public const string UnexpectedFailureCode = "ER-READER-INTERNAL-001";

    public static ReaderOperationSummary Create(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        ReaderDiagnostic diagnostic = new(
            UnexpectedFailureCode,
            ReaderDiagnosticSeverity.InternalError,
            ReaderDiagnosticArea.Reader,
            "EReader ha rilevato un errore interno inatteso durante l'operazione corrente.",
            ReaderRecoveryAction.AbortOperation,
            technicalDetails: exception.ToString());

        return new ReaderOperationSummary(
            ReaderOperationStatus.InternalFailure,
            [diagnostic]);
    }

    public static bool IsContainable(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is not OutOfMemoryException
            and not StackOverflowException
            and not AccessViolationException;
    }
}
