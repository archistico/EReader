using System.Globalization;
using System.Text;
using EbookReader.Layout;

namespace EbookReader.Cli.Tui;

/// <summary>
/// Formats format-neutral metadata rows into terminal-width-aware text lines without Terminal.Gui dependencies.
/// </summary>
public static class ReaderMetadataFormatter
{
    public static string[] Format(IReadOnlyList<ReaderMetadataEntry> entries, int width)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 2);

        List<string> lines = [];
        foreach (ReaderMetadataEntry entry in entries)
        {
            AppendEntry(lines, entry, width);
        }

        return [.. lines];
    }

    private static void AppendEntry(List<string> lines, ReaderMetadataEntry entry, int width)
    {
        string prefix = $"{entry.Label}: ";
        int prefixWidth = TerminalCellWidth.Measure(prefix);

        if (prefixWidth >= width - 1)
        {
            AppendWrapped(lines, $"{entry.Label}:", width, string.Empty, string.Empty);
            string indentation = width > 2 ? "  " : string.Empty;
            foreach (string logicalLine in SplitLogicalLines(entry.Value))
            {
                AppendWrapped(lines, logicalLine, width, indentation, indentation);
            }

            return;
        }

        bool firstLogicalLine = true;
        string continuationPrefix = new(' ', prefixWidth);
        foreach (string logicalLine in SplitLogicalLines(entry.Value))
        {
            AppendWrapped(
                lines,
                logicalLine,
                width,
                firstLogicalLine ? prefix : continuationPrefix,
                continuationPrefix);
            firstLogicalLine = false;
        }
    }

    private static string[] SplitLogicalLines(string value)
    {
        string normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        return normalized.Split('\n');
    }

    private static void AppendWrapped(
        List<string> destination,
        string value,
        int width,
        string firstPrefix,
        string continuationPrefix)
    {
        string[] words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            destination.Add(firstPrefix.TrimEnd());
            return;
        }

        StringBuilder line = new(firstPrefix);
        int lineWidth = TerminalCellWidth.Measure(firstPrefix);
        bool hasWord = false;

        foreach (string word in words)
        {
            int separatorWidth = hasWord ? 1 : 0;
            int wordWidth = TerminalCellWidth.Measure(word);
            if (lineWidth + separatorWidth + wordWidth <= width)
            {
                if (hasWord)
                {
                    line.Append(' ');
                    lineWidth++;
                }

                line.Append(word);
                lineWidth += wordWidth;
                hasWord = true;
                continue;
            }

            if (hasWord)
            {
                destination.Add(line.ToString());
                line.Clear();
                line.Append(continuationPrefix);
                lineWidth = TerminalCellWidth.Measure(continuationPrefix);
                hasWord = false;
            }

            if (lineWidth + wordWidth <= width)
            {
                line.Append(word);
                lineWidth += wordWidth;
                hasWord = true;
                continue;
            }

            AppendLongWord(destination, word, width, continuationPrefix, line, ref lineWidth, ref hasWord);
        }

        if (hasWord || line.Length > 0)
        {
            destination.Add(line.ToString().TrimEnd());
        }
    }

    private static void AppendLongWord(
        List<string> destination,
        string word,
        int width,
        string continuationPrefix,
        StringBuilder line,
        ref int lineWidth,
        ref bool hasWord)
    {
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(word);
        while (enumerator.MoveNext())
        {
            string element = enumerator.GetTextElement();
            int elementWidth = TerminalCellWidth.Measure(element);
            if (lineWidth + elementWidth > width && lineWidth > TerminalCellWidth.Measure(continuationPrefix))
            {
                destination.Add(line.ToString());
                line.Clear();
                line.Append(continuationPrefix);
                lineWidth = TerminalCellWidth.Measure(continuationPrefix);
            }

            line.Append(element);
            lineWidth += elementWidth;
            hasWord = true;
        }
    }
}
