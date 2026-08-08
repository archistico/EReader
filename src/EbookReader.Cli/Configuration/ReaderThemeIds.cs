namespace EbookReader.Cli.Configuration;

public static class ReaderThemeIds
{
    public const string SemanticDark = "semantic-dark";
    public const string PaperLight = "paper-light";
    public const string Monochrome = "monochrome";

    public static bool IsKnown(string themeId) =>
        string.Equals(themeId, SemanticDark, StringComparison.Ordinal)
        || string.Equals(themeId, PaperLight, StringComparison.Ordinal)
        || string.Equals(themeId, Monochrome, StringComparison.Ordinal);
}
