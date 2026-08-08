using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EbookReader.Cli.Tests;

public sealed class CrashContainmentTests
{
    [Fact]
    public void UnexpectedFailureIsContainedAtCliBoundary()
    {
        using ThrowingTextWriter output = new(new InvalidOperationException("m3.12 synthetic failure"));
        using StringWriter error = new();

        int exitCode = CliEntryPoint.Run(["--help"], output, error);

        string diagnostics = error.ToString();
        Assert.Equal(6, exitCode);
        Assert.Contains("[INTERNAL ER-READER-INTERNAL-001]", diagnostics, StringComparison.Ordinal);
        Assert.Contains("[INTERNAL_FAILURE]", diagnostics, StringComparison.Ordinal);
        Assert.Contains("non dimostra che l'EPUB sia illeggibile", diagnostics, StringComparison.Ordinal);
        Assert.Contains("stato di lettura già salvato resta il riferimento persistente", diagnostics, StringComparison.Ordinal);
        Assert.Contains("--diagnostics", diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain("System.InvalidOperationException", diagnostics, StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticsModifierExposesTechnicalDetailsOnlyWhenRequested()
    {
        using ThrowingTextWriter output = new(new InvalidOperationException("m3.12 synthetic technical detail"));
        using StringWriter error = new();

        int exitCode = CliEntryPoint.Run(["--diagnostics", "--help"], output, error);

        string diagnostics = error.ToString();
        Assert.Equal(6, exitCode);
        Assert.Contains("[TECHNICAL_DETAILS ER-READER-INTERNAL-001]", diagnostics, StringComparison.Ordinal);
        Assert.Contains("System.InvalidOperationException", diagnostics, StringComparison.Ordinal);
        Assert.Contains("m3.12 synthetic technical detail", diagnostics, StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticsModifierDoesNotChangeSuccessfulCommandOutcome()
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = CliEntryPoint.Run(["--diagnostics", "--version"], output, error);

        Assert.Equal(0, exitCode);
        Assert.NotEmpty(output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    [SuppressMessage(
        "Usage",
        "CA2201:Do not raise reserved exception types",
        Justification = "This test intentionally constructs OutOfMemoryException to verify that process-critical failures bypass ordinary crash containment.")]
    public void ProcessCriticalFailureIsNotContainedAsOrdinaryInternalFailure()
    {
        using ThrowingTextWriter output = new(new OutOfMemoryException("synthetic critical failure"));
        using StringWriter error = new();

        Assert.Throws<OutOfMemoryException>(() => CliEntryPoint.Run(["--help"], output, error));
        Assert.Equal(string.Empty, error.ToString());
    }

    private sealed class ThrowingTextWriter(Exception exception) : TextWriter
    {
        public override Encoding Encoding => System.Text.Encoding.UTF8;

        [SuppressMessage(
            "Usage",
            "CA2201:Do not raise reserved exception types",
            Justification = "Synthetic test writer must throw the injected exception to exercise the outer crash boundary, including process-critical exclusions.")]
        public override void Write(char value) => throw exception;
    }
}
