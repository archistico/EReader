namespace EbookReader.Epub.Validation;

/// <summary>
/// Stable, user-facing diagnostic produced while validating an EPUB publication.
/// </summary>
public sealed record EpubDiagnostic
{
    public EpubDiagnostic(
        string code,
        EpubDiagnosticSeverity severity,
        EpubDiagnosticCategory category,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Severity = severity;
        Category = category;
        Message = message;
    }

    public string Code { get; }

    public EpubDiagnosticSeverity Severity { get; }

    public EpubDiagnosticCategory Category { get; }

    public string Message { get; }
}
