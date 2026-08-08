using EbookReader.Application.Diagnostics;

namespace EbookReader.Cli.Diagnostics;

internal static class ReaderDiagnosticTextWriter
{
    public static void Write(
        ReaderOperationSummary summary,
        TextWriter error,
        bool includeTechnicalDetails = false)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(error);

        foreach (ReaderDiagnostic diagnostic in summary.Diagnostics)
        {
            error.Write('[');
            error.Write(Label(diagnostic.Severity));
            error.Write(' ');
            error.Write(diagnostic.Code);
            error.Write("] ");
            error.WriteLine(diagnostic.Message);
        }

        if (summary.Status == ReaderOperationStatus.SuccessWithDiagnostics &&
            summary.Diagnostics.Any(diagnostic => diagnostic.Severity == ReaderDiagnosticSeverity.RecoverableError))
        {
            error.WriteLine("[READABLE_DEGRADED] Il libro è leggibile, ma una o più parti non sono disponibili.");
            error.WriteLine("EReader continua usando solo contenuto verificato; non cerca sostituti fuori dall'EPUB e non modifica il file originale.");
        }
        else if (summary.Status == ReaderOperationStatus.SuccessWithDiagnostics &&
                 summary.Diagnostics.Any(diagnostic => diagnostic.Severity == ReaderDiagnosticSeverity.Warning))
        {
            error.WriteLine("[READABLE_WITH_WARNINGS] Il libro è leggibile con avvisi non bloccanti.");
        }

        if (summary.Status == ReaderOperationStatus.DocumentUnreadable)
        {
            error.WriteLine("[DOCUMENT_UNREADABLE] Impossibile aprire il libro in modo affidabile.");
            error.WriteLine("EReader ha rifiutato questo documento; il file EPUB non è stato modificato e lo stato di lettura esistente non viene aggiornato.");
        }
        else if (summary.Status == ReaderOperationStatus.InternalFailure)
        {
            error.WriteLine("[INTERNAL_FAILURE] EReader ha interrotto l'operazione corrente per un errore interno inatteso.");
            error.WriteLine("Questo errore non dimostra che l'EPUB sia illeggibile. Se l'operazione riguardava un EPUB, il file non viene modificato; lo stato di lettura già salvato resta il riferimento persistente.");

            if (includeTechnicalDetails)
            {
                WriteTechnicalDetails(summary.Diagnostics, error);
            }
            else
            {
                error.WriteLine("Ripeti il comando con --diagnostics per mostrare i dettagli tecnici.");
            }
        }
    }

    private static void WriteTechnicalDetails(IReadOnlyList<ReaderDiagnostic> diagnostics, TextWriter error)
    {
        foreach (ReaderDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic.TechnicalDetails is null)
            {
                continue;
            }

            error.Write("[TECHNICAL_DETAILS ");
            error.Write(diagnostic.Code);
            error.WriteLine("]");
            error.WriteLine(diagnostic.TechnicalDetails);
        }
    }

    private static string Label(ReaderDiagnosticSeverity severity) => severity switch
    {
        ReaderDiagnosticSeverity.Information => "INFO",
        ReaderDiagnosticSeverity.Warning => "WARNING",
        ReaderDiagnosticSeverity.RecoverableError => "RECOVERABLE",
        ReaderDiagnosticSeverity.FatalDocumentError => "DOCUMENT-UNREADABLE",
        ReaderDiagnosticSeverity.InternalError => "INTERNAL",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Severità diagnostica non supportata."),
    };
}
