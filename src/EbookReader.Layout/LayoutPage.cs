namespace EbookReader.Layout;

/// <summary>
/// Ephemeral page for one viewport. Page numbers are deliberately not Domain locations.
/// </summary>
public sealed class LayoutPage
{
    internal LayoutPage(int number, IReadOnlyList<VisualLine> lines)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);

        ArgumentNullException.ThrowIfNull(lines);
        Number = number;
        Lines = lines;
    }

    public int Number { get; }

    public IReadOnlyList<VisualLine> Lines { get; }
}
