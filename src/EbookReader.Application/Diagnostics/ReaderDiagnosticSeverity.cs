namespace EbookReader.Application.Diagnostics;

/// <summary>
/// Application-wide severity for diagnostics shown or recorded by EReader.
/// FatalDocumentError is fatal for the current document, not for the EReader process.
/// </summary>
public enum ReaderDiagnosticSeverity
{
    Information = 0,
    Warning = 1,
    RecoverableError = 2,
    FatalDocumentError = 3,
    InternalError = 4,
}
