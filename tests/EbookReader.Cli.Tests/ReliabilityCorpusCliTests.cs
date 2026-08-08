namespace EbookReader.Cli.Tests;

public sealed class ReliabilityCorpusCliTests
{
    [Theory]
    [InlineData("valid-minimal.epub", 0, "")]
    [InlineData("missing-navigation.epub", 0, "[READABLE_DEGRADED]")]
    [InlineData("unsafe-external-link.epub", 0, "[READABLE_WITH_WARNINGS]")]
    [InlineData("malformed-opf.epub", 3, "[DOCUMENT_UNREADABLE]")]
    [InlineData("unsupported-encryption.epub", 4, "[DOCUMENT_UNREADABLE]")]
    [InlineData("compression-ratio-bomb.epub", 3, "[DOCUMENT_UNREADABLE]")]
    public void PlainCliClassifiesCorpusFixtureWithoutInternalFailure(
        string fileName,
        int expectedExitCode,
        string expectedOutcomeMarker)
    {
        using StringWriter output = new();
        using StringWriter error = new();

        int exitCode = CliEntryPoint.Run(["--plain", CorpusPath(fileName)], output, error);

        string diagnostics = error.ToString();
        Assert.Equal(expectedExitCode, exitCode);
        Assert.DoesNotContain("[INTERNAL_FAILURE]", diagnostics, StringComparison.Ordinal);

        if (expectedOutcomeMarker.Length == 0)
        {
            Assert.Equal(string.Empty, diagnostics);
        }
        else
        {
            Assert.Contains(expectedOutcomeMarker, diagnostics, StringComparison.Ordinal);
        }
    }

    private static string CorpusPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "reliability", fileName);
}
