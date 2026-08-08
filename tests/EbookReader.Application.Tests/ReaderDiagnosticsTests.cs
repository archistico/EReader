using EbookReader.Application.Diagnostics;

namespace EbookReader.Application.Tests;

public sealed class ReaderDiagnosticsTests
{
    [Fact]
    public void DiagnosticStoresStableFormatNeutralContract()
    {
        ReaderDiagnostic diagnostic = new(
            "ER-TEST-001",
            ReaderDiagnosticSeverity.RecoverableError,
            ReaderDiagnosticArea.Resource,
            "Risorsa non disponibile.",
            ReaderRecoveryAction.ContinueDegraded,
            "images/cover.png",
            "decoder=future");

        Assert.Equal("ER-TEST-001", diagnostic.Code);
        Assert.Equal(ReaderDiagnosticSeverity.RecoverableError, diagnostic.Severity);
        Assert.Equal(ReaderDiagnosticArea.Resource, diagnostic.Area);
        Assert.Equal("Risorsa non disponibile.", diagnostic.Message);
        Assert.Equal(ReaderRecoveryAction.ContinueDegraded, diagnostic.RecoveryAction);
        Assert.Equal("images/cover.png", diagnostic.Resource);
        Assert.Equal("decoder=future", diagnostic.TechnicalDetails);
    }

    [Fact]
    public void DiagnosticRejectsWhitespaceInsideMachineReadableCode()
    {
        Assert.Throws<ArgumentException>(() => new ReaderDiagnostic(
            "ER TEST 001",
            ReaderDiagnosticSeverity.Warning,
            ReaderDiagnosticArea.Reader,
            "Messaggio."));
    }

    [Fact]
    public void DiagnosticNormalizesOptionalContext()
    {
        ReaderDiagnostic diagnostic = new(
            "ER-TEST-002",
            ReaderDiagnosticSeverity.Information,
            ReaderDiagnosticArea.Publication,
            "Informazione.",
            resource: "  OPS/ch1.xhtml  ",
            technicalDetails: "   ");

        Assert.Equal("OPS/ch1.xhtml", diagnostic.Resource);
        Assert.Null(diagnostic.TechnicalDetails);
    }

    [Fact]
    public void SuccessCannotHideDiagnostics()
    {
        ReaderDiagnostic diagnostic = Warning();

        Assert.Throws<ArgumentException>(() => new ReaderOperationSummary(
            ReaderOperationStatus.Success,
            [diagnostic]));
    }

    [Fact]
    public void SuccessWithDiagnosticsAcceptsOnlyNonFatalDiagnostics()
    {
        ReaderOperationSummary summary = new(
            ReaderOperationStatus.SuccessWithDiagnostics,
            [Warning()]);

        Assert.True(summary.CanContinue);
        Assert.Single(summary.Diagnostics);

        Assert.Throws<ArgumentException>(() => new ReaderOperationSummary(
            ReaderOperationStatus.SuccessWithDiagnostics,
            [Fatal()]));
    }

    [Fact]
    public void DocumentUnreadableRequiresFatalDocumentDiagnostic()
    {
        ReaderOperationSummary summary = new(
            ReaderOperationStatus.DocumentUnreadable,
            [Warning(), Fatal()]);

        Assert.False(summary.CanContinue);
        Assert.Equal(2, summary.Diagnostics.Count);

        Assert.Throws<ArgumentException>(() => new ReaderOperationSummary(
            ReaderOperationStatus.DocumentUnreadable,
            [Warning()]));
    }

    [Fact]
    public void InternalFailureRequiresInternalDiagnostic()
    {
        ReaderDiagnostic internalError = new(
            "ER-READER-INTERNAL-001",
            ReaderDiagnosticSeverity.InternalError,
            ReaderDiagnosticArea.Reader,
            "Errore interno.");

        ReaderOperationSummary summary = new(
            ReaderOperationStatus.InternalFailure,
            [internalError]);

        Assert.False(summary.CanContinue);
        Assert.Throws<ArgumentException>(() => new ReaderOperationSummary(
            ReaderOperationStatus.InternalFailure,
            [Fatal()]));
    }

    [Fact]
    public void EmptySuccessIsTheOnlyDiagnosticFreeSuccessOutcome()
    {
        ReaderOperationSummary summary = new(ReaderOperationStatus.Success);

        Assert.True(summary.CanContinue);
        Assert.Empty(summary.Diagnostics);

        Assert.Throws<ArgumentException>(() => new ReaderOperationSummary(
            ReaderOperationStatus.SuccessWithDiagnostics));
    }

    private static ReaderDiagnostic Warning() => new(
        "ER-TEST-WARNING-001",
        ReaderDiagnosticSeverity.Warning,
        ReaderDiagnosticArea.Publication,
        "Warning.",
        ReaderRecoveryAction.Continue);

    private static ReaderDiagnostic Fatal() => new(
        "ER-TEST-FATAL-001",
        ReaderDiagnosticSeverity.FatalDocumentError,
        ReaderDiagnosticArea.Publication,
        "Documento illeggibile.",
        ReaderRecoveryAction.RejectDocument);
}
