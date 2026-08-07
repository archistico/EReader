namespace EbookReader.Layout;

/// <summary>
/// Presentation hint retained by the layout without introducing UI framework types.
/// </summary>
public enum VisualLineKind
{
    Body,
    Heading,
    Quote,
    ListItem,
    Preformatted,
    Image,
    ThematicBreak,
    Spacing,
}
