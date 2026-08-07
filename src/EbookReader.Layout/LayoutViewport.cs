namespace EbookReader.Layout;

/// <summary>
/// Terminal-independent viewport measured in character cells and visual lines.
/// </summary>
public sealed record LayoutViewport
{
    public LayoutViewport(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }
}
