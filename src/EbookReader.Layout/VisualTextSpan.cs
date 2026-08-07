namespace EbookReader.Layout;

/// <summary>
/// A styled UTF-16 range inside <see cref="VisualLine.Text"/>.
/// </summary>
public sealed class VisualTextSpan
{
    public VisualTextSpan(int startIndex, int length, VisualTextStyle style)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

        if (style == VisualTextStyle.None || (style & ~(VisualTextStyle.Strong | VisualTextStyle.Emphasis)) != VisualTextStyle.None)
        {
            throw new ArgumentOutOfRangeException(nameof(style));
        }

        StartIndex = startIndex;
        Length = length;
        Style = style;
    }

    public int StartIndex { get; }

    public int Length { get; }

    public int EndIndex => StartIndex + Length;

    public VisualTextStyle Style { get; }
}
