namespace EbookReader.Cli.State;

internal static class ReadingStatePathResolver
{
    private const string OverrideEnvironmentVariable = "EREADER_STATE_FILE";

    public static string Resolve()
    {
        string? overridePath = Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("Impossibile determinare la directory LocalApplicationData.");
        }

        return Path.Combine(localApplicationData, "EReader", "state.json");
    }
}
