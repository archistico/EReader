namespace EbookReader.Layout;

/// <summary>
/// Ephemeral coordinate inside one concrete BookLayout. It must never be persisted as reading state.
/// </summary>
public sealed record LayoutPosition
{
    public LayoutPosition(int pageNumber, int lineIndex)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(lineIndex);
        PageNumber = pageNumber;
        LineIndex = lineIndex;
    }

    public int PageNumber { get; }

    public int LineIndex { get; }
}
