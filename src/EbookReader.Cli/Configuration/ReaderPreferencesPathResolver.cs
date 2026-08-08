namespace EbookReader.Cli.Configuration;

internal static class ReaderPreferencesPathResolver
{
    private const string OverrideEnvironmentVariable = "EREADER_CONFIG_FILE";

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

        return Path.Combine(localApplicationData, "EReader", "config.json");
    }
}
