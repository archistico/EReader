using EbookReader.Cli.Configuration;
using EbookReader.Layout;
using Terminal.Gui.Drawing;
using TuiAttribute = Terminal.Gui.Drawing.Attribute;

namespace EbookReader.Cli.Tui;

/// <summary>
/// Terminal.Gui-only palette for one reader theme. Semantic text roles remain in EbookReader.Layout;
/// this type only maps those roles to concrete terminal attributes.
/// </summary>
internal sealed class ReaderTheme
{
    public ReaderTheme(
        string id,
        string name,
        TuiAttribute plainText,
        TuiAttribute chapterHeading,
        TuiAttribute strongText,
        TuiAttribute emphasisText,
        TuiAttribute strongEmphasisText,
        TuiAttribute chrome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!ReaderThemeIds.IsKnown(id))
        {
            throw new ArgumentException($"Identificatore tema sconosciuto: {id}.", nameof(id));
        }

        Id = id;
        Name = name;
        PlainText = plainText;
        ChapterHeading = chapterHeading;
        StrongText = strongText;
        EmphasisText = emphasisText;
        StrongEmphasisText = strongEmphasisText;
        Chrome = chrome;
        PlainScheme = new Scheme(plainText);
        ChromeScheme = new Scheme(chrome);
    }

    public string Id { get; }

    public string Name { get; }

    public TuiAttribute PlainText { get; }

    public TuiAttribute ChapterHeading { get; }

    public TuiAttribute StrongText { get; }

    public TuiAttribute EmphasisText { get; }

    public TuiAttribute StrongEmphasisText { get; }

    public TuiAttribute Chrome { get; }

    public Scheme PlainScheme { get; }

    public Scheme ChromeScheme { get; }

    public TuiAttribute ForStyle(VisualTextStyle style) => style switch
    {
        VisualTextStyle.Strong => StrongText,
        VisualTextStyle.Emphasis => EmphasisText,
        VisualTextStyle.StrongEmphasis => StrongEmphasisText,
        _ => PlainText,
    };
}
