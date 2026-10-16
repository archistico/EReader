using System.Text.Json;

namespace EbookReader.Cli.Tests;

[CollectionDefinition("Reading state environment", DisableParallelization = true)]
public sealed class ReadingStateEnvironmentDefinition;

[Collection("Reading state environment")]
public sealed class ReadingStateCliTests
{
    [Theory]
    [InlineData("bookmarks")]
    [InlineData("history")]
    [InlineData("highlights")]
    [InlineData("notes")]
    public void NullStateCollectionItemIsIgnoredWithStateDiagnostic(string collection)
    {
        string path = Path.Combine(Path.GetTempPath(), $"ereader-null-state-{Guid.NewGuid():N}.json");
        string? previousStatePath = Environment.GetEnvironmentVariable("EREADER_STATE_FILE");
        var document = new Dictionary<string, object?>
        {
            ["schemaVersion"] = 4,
            ["lastBook"] = new
            {
                path = Path.Combine(Path.GetTempPath(), "book.epub"),
                bookId = "book",
                lastOpenedUtc = "2026-10-16T12:00:00Z",
                location = new { sectionId = "one", characterOffset = 0 },
            },
            [collection] = new object?[] { null },
        };
        try
        {
            string json = JsonSerializer.Serialize(document);
            File.WriteAllText(path, json);
            Environment.SetEnvironmentVariable("EREADER_STATE_FILE", path);
            using StringWriter output = new();
            using StringWriter error = new();

            int exitCode = CliEntryPoint.Run(["--diagnostics", "--history"], output, error);

            Assert.Equal(0, exitCode);
            Assert.Contains("Nessun libro recente.", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("ER-STATE-LOAD-001", error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("[INTERNAL_FAILURE]", error.ToString(), StringComparison.Ordinal);
            Assert.Equal(json, File.ReadAllText(path));
        }
        finally
        {
            Environment.SetEnvironmentVariable("EREADER_STATE_FILE", previousStatePath);
            File.Delete(path);
        }
    }
}
