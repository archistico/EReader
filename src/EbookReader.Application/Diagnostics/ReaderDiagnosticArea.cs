namespace EbookReader.Application.Diagnostics;

/// <summary>
/// Format-neutral application area that produced a diagnostic.
/// </summary>
public enum ReaderDiagnosticArea
{
    Publication = 0,
    Navigation = 1,
    Content = 2,
    Resource = 3,
    Persistence = 4,
    Configuration = 5,
    Reader = 6,
}
