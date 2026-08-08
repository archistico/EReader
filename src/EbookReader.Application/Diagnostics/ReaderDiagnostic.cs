namespace EbookReader.Application.Diagnostics;

/// <summary>
/// Stable format-neutral diagnostic used beyond adapter-specific parser boundaries.
/// </summary>
public sealed record ReaderDiagnostic
{
    public ReaderDiagnostic(
        string code,
        ReaderDiagnosticSeverity severity,
        ReaderDiagnosticArea area,
        string message,
        ReaderRecoveryAction recoveryAction = ReaderRecoveryAction.None,
        string? resource = null,
        string? technicalDetails = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (code.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Il codice diagnostico non può contenere spazi.", nameof(code));
        }

        Code = code;
        Severity = severity;
        Area = area;
        Message = message;
        RecoveryAction = recoveryAction;
        Resource = NormalizeOptional(resource);
        TechnicalDetails = NormalizeOptional(technicalDetails);
    }

    public string Code { get; }

    public ReaderDiagnosticSeverity Severity { get; }

    public ReaderDiagnosticArea Area { get; }

    public string Message { get; }

    public ReaderRecoveryAction RecoveryAction { get; }

    /// <summary>
    /// Optional logical/virtual resource identifier. It must not be interpreted as a layout coordinate.
    /// </summary>
    public string? Resource { get; }

    /// <summary>
    /// Optional technical context kept separate from the primary user-facing message.
    /// </summary>
    public string? TechnicalDetails { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
