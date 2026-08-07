using EbookReader.Domain.Books;
using EbookReader.Domain.Reading;

namespace EbookReader.Layout;

/// <summary>
/// Moves through the ephemeral visual projection while always returning stable logical locations.
/// </summary>
public static class LayoutNavigator
{
    public static ReadingLocation? NextLine(Book book, BookLayout layout, ReadingLocation current) =>
        MoveMappedLine(book, layout, current, +1);

    public static ReadingLocation? PreviousLine(Book book, BookLayout layout, ReadingLocation current) =>
        MoveMappedLine(book, layout, current, -1);

    public static ReadingLocation? NextPage(Book book, BookLayout layout, ReadingLocation current) =>
        MovePage(book, layout, current, +1);

    public static ReadingLocation? PreviousPage(Book book, BookLayout layout, ReadingLocation current) =>
        MovePage(book, layout, current, -1);

    private static ReadingLocation? MoveMappedLine(
        Book book,
        BookLayout layout,
        ReadingLocation current,
        int delta)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(current);

        LayoutPosition position = LayoutLocationResolver.Locate(book, layout, current);
        List<(LayoutPosition Position, VisualLine Line)> mapped = LayoutLocationResolver.EnumerateMappedLines(layout).ToList();
        int index = mapped.FindIndex(item => item.Position == position);
        if (index < 0)
        {
            return null;
        }

        int destination = index + delta;
        if (destination < 0 || destination >= mapped.Count)
        {
            return null;
        }

        return mapped[destination].Line.StartLocation;
    }

    private static ReadingLocation? MovePage(
        Book book,
        BookLayout layout,
        ReadingLocation current,
        int delta)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(current);

        LayoutPosition position = LayoutLocationResolver.Locate(book, layout, current);
        int destinationPage = position.PageNumber + delta;
        if (destinationPage < 1 || destinationPage > layout.Pages.Count)
        {
            return null;
        }

        LayoutPage page = layout.Pages[destinationPage - 1];
        for (int lineIndex = 0; lineIndex < page.Lines.Count; lineIndex++)
        {
            VisualLine line = page.Lines[lineIndex];
            if (line.StartLocation is ReadingLocation location)
            {
                return location;
            }
        }

        return null;
    }
}
