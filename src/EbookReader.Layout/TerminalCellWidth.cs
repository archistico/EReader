using System.Globalization;
using System.Text;

namespace EbookReader.Layout;

/// <summary>
/// Deterministic approximation of terminal cell width over Unicode grapheme clusters.
/// </summary>
public static class TerminalCellWidth
{
    public static int Measure(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        int width = 0;
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(value);
        while (enumerator.MoveNext())
        {
            width = checked(width + MeasureTextElement(enumerator.GetTextElement()));
        }

        return width;
    }

    internal static IReadOnlyList<LayoutTextElement> Enumerate(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        List<LayoutTextElement> elements = [];
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(value);
        while (enumerator.MoveNext())
        {
            string text = enumerator.GetTextElement();
            elements.Add(
                new LayoutTextElement(
                    text,
                    MeasureTextElement(text),
                    IsWhitespace(text),
                    enumerator.ElementIndex,
                    text.Length));
        }

        return elements;
    }

    internal static string Truncate(string value, int maximumWidth)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumWidth);

        StringBuilder result = new();
        int width = 0;
        foreach (LayoutTextElement element in Enumerate(value))
        {
            if (width + element.Width > maximumWidth)
            {
                break;
            }

            result.Append(element.Text);
            width += element.Width;
        }

        return result.ToString();
    }

    private static int MeasureTextElement(string textElement)
    {
        int width = 0;
        foreach (Rune rune in textElement.EnumerateRunes())
        {
            width = Math.Max(width, MeasureRune(rune));
        }

        return width;
    }

    private static int MeasureRune(Rune rune)
    {
        UnicodeCategory category = Rune.GetUnicodeCategory(rune);
        if (category is UnicodeCategory.Control
            or UnicodeCategory.Format
            or UnicodeCategory.NonSpacingMark
            or UnicodeCategory.EnclosingMark
            or UnicodeCategory.SpacingCombiningMark)
        {
            return 0;
        }

        return IsWide(rune.Value) ? 2 : 1;
    }

    private static bool IsWhitespace(string textElement)
    {
        foreach (Rune rune in textElement.EnumerateRunes())
        {
            if (!Rune.IsWhiteSpace(rune))
            {
                return false;
            }
        }

        return textElement.Length > 0;
    }

    private static bool IsWide(int value) =>
        value is >= 0x1100 and <= 0x115F
        or 0x2329 or 0x232A
        or >= 0x2E80 and <= 0xA4CF
        or >= 0xAC00 and <= 0xD7A3
        or >= 0xF900 and <= 0xFAFF
        or >= 0xFE10 and <= 0xFE19
        or >= 0xFE30 and <= 0xFE6F
        or >= 0xFF00 and <= 0xFF60
        or >= 0xFFE0 and <= 0xFFE6
        or >= 0x1F1E6 and <= 0x1F1FF
        or >= 0x1F300 and <= 0x1FAFF
        or >= 0x20000 and <= 0x3FFFD;
}

internal readonly record struct LayoutTextElement(
    string Text,
    int Width,
    bool IsWhitespace,
    int SourceStartOffset,
    int SourceLength)
{
    public int SourceEndOffset => SourceStartOffset + SourceLength;
}
