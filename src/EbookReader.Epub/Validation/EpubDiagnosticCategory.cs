namespace EbookReader.Epub.Validation;

/// <summary>
/// Pipeline boundary that produced an EPUB diagnostic.
/// </summary>
public enum EpubDiagnosticCategory
{
    Container = 0,
    Protection = 1,
    Package = 2,
    Navigation = 3,
    Content = 4,
}
