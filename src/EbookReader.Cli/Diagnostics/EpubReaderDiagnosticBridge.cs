using EbookReader.Application.Diagnostics;
using EbookReader.Epub.Validation;

namespace EbookReader.Cli.Diagnostics;

/// <summary>
/// Composition-root bridge from EPUB-adapter diagnostics to the format-neutral application taxonomy.
/// </summary>
internal static class EpubReaderDiagnosticBridge
{
    public static ReaderOperationSummary Create(EpubValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        ReaderDiagnostic[] diagnostics = result.Diagnostics
            .Select(diagnostic => Map(diagnostic, result.Status))
            .ToArray();

        ReaderOperationStatus status = result.Status switch
        {
            EpubValidationStatus.Valid when diagnostics.Length == 0 => ReaderOperationStatus.Success,
            EpubValidationStatus.Valid => ReaderOperationStatus.SuccessWithDiagnostics,
            EpubValidationStatus.Invalid => ReaderOperationStatus.DocumentUnreadable,
            EpubValidationStatus.Unsupported => ReaderOperationStatus.DocumentUnreadable,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, "Stato EPUB non supportato."),
        };

        return new ReaderOperationSummary(status, diagnostics);
    }

    private static ReaderDiagnostic Map(EpubDiagnostic diagnostic, EpubValidationStatus validationStatus)
    {
        ReaderDiagnosticSeverity severity = diagnostic.Severity switch
        {
            EpubDiagnosticSeverity.Information => ReaderDiagnosticSeverity.Information,
            EpubDiagnosticSeverity.Warning => ReaderDiagnosticSeverity.Warning,
            EpubDiagnosticSeverity.Error when validationStatus == EpubValidationStatus.Valid =>
                ReaderDiagnosticSeverity.RecoverableError,
            EpubDiagnosticSeverity.Error => ReaderDiagnosticSeverity.FatalDocumentError,
            _ => throw new ArgumentOutOfRangeException(nameof(diagnostic), diagnostic.Severity, "Severità EPUB non supportata."),
        };

        return new ReaderDiagnostic(
            diagnostic.Code,
            severity,
            MapArea(diagnostic.Category),
            diagnostic.Message,
            severity switch
            {
                ReaderDiagnosticSeverity.Information => ReaderRecoveryAction.Continue,
                ReaderDiagnosticSeverity.Warning => ReaderRecoveryAction.Continue,
                ReaderDiagnosticSeverity.RecoverableError => ReaderRecoveryAction.ContinueDegraded,
                ReaderDiagnosticSeverity.FatalDocumentError => ReaderRecoveryAction.RejectDocument,
                _ => ReaderRecoveryAction.None,
            });
    }

    private static ReaderDiagnosticArea MapArea(EpubDiagnosticCategory category) => category switch
    {
        EpubDiagnosticCategory.Container => ReaderDiagnosticArea.Publication,
        EpubDiagnosticCategory.Protection => ReaderDiagnosticArea.Publication,
        EpubDiagnosticCategory.Package => ReaderDiagnosticArea.Publication,
        EpubDiagnosticCategory.Navigation => ReaderDiagnosticArea.Navigation,
        EpubDiagnosticCategory.Content => ReaderDiagnosticArea.Content,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Categoria EPUB non supportata."),
    };
}
