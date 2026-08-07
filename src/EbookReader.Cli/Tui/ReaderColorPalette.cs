using EbookReader.Layout;
using Terminal.Gui.Drawing;
using TuiAttribute = Terminal.Gui.Drawing.Attribute;

namespace EbookReader.Cli.Tui;

internal static class ReaderColorPalette
{
    public static TuiAttribute PlainText { get; } = new("White", "Black");

    public static TuiAttribute ChapterHeading { get; } = new("Cyan", "Black");

    public static TuiAttribute StrongText { get; } = new("Green", "Black", TextStyle.Bold);

    public static TuiAttribute EmphasisText { get; } = new("Yellow", "Black", TextStyle.Italic);

    public static TuiAttribute StrongEmphasisText { get; } = new("Green", "Black", TextStyle.Bold | TextStyle.Italic);

    public static TuiAttribute Chrome { get; } = new("Gray", "Black");

    public static Scheme PlainScheme { get; } = new(PlainText);

    public static Scheme ChromeScheme { get; } = new(Chrome);

    public static TuiAttribute ForStyle(VisualTextStyle style) => style switch
    {
        VisualTextStyle.Strong => StrongText,
        VisualTextStyle.Emphasis => EmphasisText,
        VisualTextStyle.StrongEmphasis => StrongEmphasisText,
        _ => PlainText,
    };
}
