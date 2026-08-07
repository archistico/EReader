namespace EbookReader.Layout;

/// <summary>
/// Deterministic visual projection of a Domain book for one viewport.
/// </summary>
public sealed class BookLayout
{
    internal BookLayout(LayoutViewport viewport, IReadOnlyList<LayoutPage> pages)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(pages);
        Viewport = viewport;
        Pages = pages;
    }

    public LayoutViewport Viewport { get; }

    public IReadOnlyList<LayoutPage> Pages { get; }

    public int VisualLineCount => Pages.Sum(page => page.Lines.Count);
}
