using System.Collections.ObjectModel;
using EbookReader.Cli.Configuration;
using Terminal.Gui.Drawing;
using TuiAttribute = Terminal.Gui.Drawing.Attribute;

namespace EbookReader.Cli.Tui;

internal static class ReaderThemeCatalog
{
    private static readonly ReaderTheme[] ThemeArray =
    [
        new ReaderTheme(
            ReaderThemeIds.SemanticDark,
            "Semantico scuro",
            ReaderColorPalette.PlainText,
            ReaderColorPalette.ChapterHeading,
            ReaderColorPalette.StrongText,
            ReaderColorPalette.EmphasisText,
            ReaderColorPalette.StrongEmphasisText,
            ReaderColorPalette.Chrome,
            ReaderColorPalette.HighlightText),
        new ReaderTheme(
            ReaderThemeIds.PaperLight,
            "Carta chiara",
            new TuiAttribute("Black", "White"),
            new TuiAttribute("Cyan", "White", TextStyle.Bold),
            new TuiAttribute("Green", "White", TextStyle.Bold),
            new TuiAttribute("Black", "White", TextStyle.Italic),
            new TuiAttribute("Green", "White", TextStyle.Bold | TextStyle.Italic),
            new TuiAttribute("Gray", "White"),
            new TuiAttribute("Black", "Yellow")),
        new ReaderTheme(
            ReaderThemeIds.Monochrome,
            "Monocromatico",
            new TuiAttribute("White", "Black"),
            new TuiAttribute("White", "Black", TextStyle.Bold),
            new TuiAttribute("White", "Black", TextStyle.Bold),
            new TuiAttribute("White", "Black", TextStyle.Italic),
            new TuiAttribute("White", "Black", TextStyle.Bold | TextStyle.Italic),
            new TuiAttribute("Gray", "Black"),
            new TuiAttribute("Black", "White")),
    ];

    public static ReadOnlyCollection<ReaderTheme> All { get; } = Array.AsReadOnly(ThemeArray);

    public static ReaderTheme Default => ThemeArray[0];

    public static int IndexOfId(string themeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themeId);
        for (int index = 0; index < ThemeArray.Length; index++)
        {
            if (string.Equals(ThemeArray[index].Id, themeId, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return 0;
    }
}
