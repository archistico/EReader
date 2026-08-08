namespace EbookReader.Cli.Configuration;

public sealed class ReaderPreferences
{
    public ReaderPreferences(string themeId, ReaderKeymap keymap)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themeId);
        ArgumentNullException.ThrowIfNull(keymap);
        if (!ReaderThemeIds.IsKnown(themeId))
        {
            throw new ArgumentException($"Tema EReader sconosciuto: {themeId}.", nameof(themeId));
        }

        ThemeId = themeId;
        Keymap = keymap;
    }

    public static ReaderPreferences Default { get; } = new(ReaderThemeIds.SemanticDark, ReaderKeymap.Default);

    public string ThemeId { get; }

    public ReaderKeymap Keymap { get; }

    public ReaderPreferences WithTheme(string themeId) => new(themeId, Keymap);
}
