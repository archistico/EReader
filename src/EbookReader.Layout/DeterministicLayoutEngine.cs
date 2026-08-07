using System.Globalization;
using System.Text;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;

namespace EbookReader.Layout;

/// <summary>
/// Converts format-neutral Domain blocks into deterministic visual lines and viewport pages.
/// </summary>
public static class DeterministicLayoutEngine
{
    private const int TabSize = 4;

    public static BookLayout Layout(Book book, LayoutViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(book);
        return LayoutSections(book.ReadingOrder, viewport);
    }

    public static BookLayout Layout(ReadingSection section, LayoutViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(section);
        return LayoutSections([section], viewport);
    }

    private static BookLayout LayoutSections(IEnumerable<ReadingSection> sections, LayoutViewport viewport)
    {
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(viewport);

        List<VisualLine> lines = [];
        foreach (ReadingSection section in sections)
        {
            ArgumentNullException.ThrowIfNull(section);
            AddSection(lines, section, viewport.Width);
        }

        while (lines.Count > 0 && lines[^1].Kind == VisualLineKind.Spacing)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return new BookLayout(viewport, Paginate(lines, viewport.Height));
    }

    private static void AddSection(List<VisualLine> target, ReadingSection section, int viewportWidth)
    {
        if (target.Count > 0)
        {
            AddSpacing(target, section.Id, blockId: null);
        }

        for (int index = 0; index < section.Blocks.Count; index++)
        {
            ContentBlock block = section.Blocks[index];
            AddBlock(target, section.Id, block, viewportWidth);

            bool nextIsListItem = index + 1 < section.Blocks.Count
                && section.Blocks[index + 1] is ListItemBlock;
            if (block is not ListItemBlock || !nextIsListItem)
            {
                AddSpacing(target, section.Id, block.Id);
            }
        }
    }

    private static void AddBlock(
        List<VisualLine> target,
        SectionId sectionId,
        ContentBlock block,
        int viewportWidth)
    {
        switch (block)
        {
            case HeadingBlock heading:
                AddFlowText(
                    target,
                    StyledContentText.FromBlock(heading),
                    string.Empty,
                    string.Empty,
                    viewportWidth,
                    VisualLineKind.Heading,
                    sectionId,
                    heading.Id,
                    heading.Level);
                break;

            case ParagraphBlock paragraph:
                AddFlowText(
                    target,
                    StyledContentText.FromBlock(paragraph),
                    string.Empty,
                    string.Empty,
                    viewportWidth,
                    VisualLineKind.Body,
                    sectionId,
                    paragraph.Id);
                break;

            case QuoteBlock quote:
                int visibleQuoteDepth = Math.Min(quote.Depth, Math.Max(0, (viewportWidth - 2) / 2));
                string quotePrefix = string.Concat(Enumerable.Repeat("> ", visibleQuoteDepth));
                AddFlowText(
                    target,
                    StyledContentText.FromBlock(quote),
                    quotePrefix,
                    quotePrefix,
                    viewportWidth,
                    VisualLineKind.Quote,
                    sectionId,
                    quote.Id);
                break;

            case ListItemBlock item:
                AddListItem(target, sectionId, item, viewportWidth);
                break;

            case PreformattedBlock preformatted:
                AddPreformatted(target, sectionId, preformatted, viewportWidth);
                break;

            case ImageBlock image:
                AddSyntheticBlockText(target, sectionId, image, viewportWidth);
                break;

            case ThematicBreakBlock thematicBreak:
                AddLine(
                    target,
                    "---",
                    VisualLineKind.ThematicBreak,
                    sectionId,
                    thematicBreak.Id,
                    sourceStartOffset: 0,
                    sourceEndOffset: 0);
                break;

            default:
                throw new NotSupportedException(
                    $"Tipo di blocco Domain non supportato dal layout M1.2: {block.GetType().FullName}.");
        }
    }

    private static void AddListItem(
        List<VisualLine> target,
        SectionId sectionId,
        ListItemBlock item,
        int viewportWidth)
    {
        string marker = item.ListKind == ListKind.Ordered
            ? item.Ordinal is int ordinal
                ? $"{ordinal.ToString(CultureInfo.InvariantCulture)}. "
                : "#. "
            : "- ";
        int maximumIndentation = Math.Max(0, viewportWidth - marker.Length - 2);
        int indentationWidth = item.Depth - 1 > maximumIndentation / 2
            ? maximumIndentation
            : (item.Depth - 1) * 2;
        string indentation = new(' ', indentationWidth);

        AddFlowText(
            target,
            StyledContentText.FromBlock(item),
            indentation + marker,
            indentation + new string(' ', marker.Length),
            viewportWidth,
            VisualLineKind.ListItem,
            sectionId,
            item.Id);
    }

    private static void AddPreformatted(
        List<VisualLine> target,
        SectionId sectionId,
        PreformattedBlock block,
        int viewportWidth)
    {
        foreach (HardLine hardLine in EnumerateHardLines(block.Text))
        {
            foreach (WrappedSegment segment in WrapPreformatted(hardLine, viewportWidth))
            {
                AddLine(
                    target,
                    segment.Text,
                    VisualLineKind.Preformatted,
                    sectionId,
                    block.Id,
                    segment.SourceStartOffset,
                    segment.SourceEndOffset);
            }
        }
    }

    private static void AddFlowText(
        List<VisualLine> target,
        StyledLogicalText styledText,
        string firstPrefix,
        string continuationPrefix,
        int viewportWidth,
        VisualLineKind kind,
        SectionId sectionId,
        BlockId blockId,
        int? headingLevel = null)
    {
        ArgumentNullException.ThrowIfNull(styledText);
        string text = styledText.Text;
        if (text.Length == 0)
        {
            return;
        }

        if (styledText.Styles.Length != text.Length)
        {
            throw new InvalidOperationException("La mappa degli stili deve avere la stessa lunghezza UTF-16 del testo logico.");
        }

        int maximumPrefixWidth = Math.Max(0, viewportWidth - 2);
        firstPrefix = TerminalCellWidth.Truncate(firstPrefix, maximumPrefixWidth);
        continuationPrefix = TerminalCellWidth.Truncate(continuationPrefix, maximumPrefixWidth);
        int prefixWidth = Math.Max(
            TerminalCellWidth.Measure(firstPrefix),
            TerminalCellWidth.Measure(continuationPrefix));
        int contentWidth = viewportWidth - prefixWidth;
        bool firstVisualLine = true;

        foreach (HardLine hardLine in EnumerateHardLines(text))
        {
            foreach (WrappedSegment segment in WrapFlow(hardLine, contentWidth, styledText.Styles))
            {
                string prefix = firstVisualLine ? firstPrefix : continuationPrefix;
                AddLine(
                    target,
                    prefix + segment.Text,
                    kind,
                    sectionId,
                    blockId,
                    segment.SourceStartOffset,
                    segment.SourceEndOffset,
                    headingLevel,
                    ShiftSpans(segment.StyleSpans, prefix.Length));
                firstVisualLine = false;
            }
        }
    }

    private static void AddSyntheticBlockText(
        List<VisualLine> target,
        SectionId sectionId,
        ImageBlock image,
        int viewportWidth)
    {
        string displayText = FormatImage(image);
        int logicalLength = ContentText.GetPlainText(image).Length;
        foreach (WrappedSegment segment in WrapFlow(new HardLine(displayText, 0, displayText.Length), viewportWidth, new VisualTextStyle[displayText.Length]))
        {
            AddLine(
                target,
                segment.Text,
                VisualLineKind.Image,
                sectionId,
                image.Id,
                sourceStartOffset: 0,
                sourceEndOffset: logicalLength);
        }
    }

    private static List<WrappedSegment> WrapFlow(HardLine hardLine, int maximumWidth, VisualTextStyle[] styles)
    {
        List<FlowWord> words = TokenizeWords(hardLine, styles);
        if (words.Count == 0)
        {
            return [new WrappedSegment(string.Empty, hardLine.StartOffset, hardLine.SeparatorEndOffset, [])];
        }

        List<WrappedSegment> lines = [];
        StringBuilder current = new();
        List<VisualTextSpan> currentSpans = [];
        int currentWidth = 0;
        int lineStart = hardLine.StartOffset;

        for (int wordIndex = 0; wordIndex < words.Count; wordIndex++)
        {
            FlowWord word = words[wordIndex];
            int wordWidth = word.Elements.Sum(element => element.Width);
            int wordStart = hardLine.StartOffset + word.SourceStartOffset;

            if (current.Length > 0 && currentWidth + 1 + wordWidth <= maximumWidth)
            {
                current.Append(' ');
                AppendStyled(current, currentSpans, word.Elements);
                currentWidth += 1 + wordWidth;
                continue;
            }

            if (current.Length > 0)
            {
                AddWrappedSegment(lines, current, currentSpans, lineStart, wordStart);
                currentWidth = 0;
                lineStart = wordStart;
            }

            for (int elementIndex = 0; elementIndex < word.Elements.Length; elementIndex++)
            {
                StyledLayoutTextElement element = word.Elements[elementIndex];
                int elementStart = hardLine.StartOffset + element.SourceStartOffset;
                if (currentWidth > 0 && currentWidth + element.Width > maximumWidth)
                {
                    AddWrappedSegment(lines, current, currentSpans, lineStart, elementStart);
                    currentWidth = 0;
                    lineStart = elementStart;
                }

                AppendStyled(current, currentSpans, element.Text, element.Style);
                currentWidth += element.Width;

                if (currentWidth == maximumWidth && elementIndex + 1 < word.Elements.Length)
                {
                    int nextStart = hardLine.StartOffset + word.Elements[elementIndex + 1].SourceStartOffset;
                    AddWrappedSegment(lines, current, currentSpans, lineStart, nextStart);
                    currentWidth = 0;
                    lineStart = nextStart;
                }
            }
        }

        if (current.Length > 0)
        {
            AddWrappedSegment(lines, current, currentSpans, lineStart, hardLine.SeparatorEndOffset);
        }

        return lines;
    }

    private static List<WrappedSegment> WrapPreformatted(HardLine hardLine, int maximumWidth)
    {
        List<DisplayElement> elements = ExpandTabs(hardLine.Text);
        if (elements.Count == 0)
        {
            return [new WrappedSegment(string.Empty, hardLine.StartOffset, hardLine.SeparatorEndOffset, [])];
        }

        List<WrappedSegment> lines = [];
        StringBuilder current = new();
        int currentWidth = 0;
        int lineStart = hardLine.StartOffset;

        for (int index = 0; index < elements.Count; index++)
        {
            DisplayElement element = elements[index];
            int elementStart = hardLine.StartOffset + element.SourceStartOffset;
            if (currentWidth > 0 && currentWidth + element.Width > maximumWidth)
            {
                lines.Add(new WrappedSegment(current.ToString(), lineStart, elementStart, []));
                current.Clear();
                currentWidth = 0;
                lineStart = elementStart;
            }

            current.Append(element.Text);
            currentWidth += element.Width;
        }

        lines.Add(new WrappedSegment(current.ToString(), lineStart, hardLine.SeparatorEndOffset, []));
        return lines;
    }

    private static List<FlowWord> TokenizeWords(HardLine hardLine, VisualTextStyle[] styles)
    {
        List<FlowWord> words = [];
        List<StyledLayoutTextElement> current = [];
        foreach (LayoutTextElement element in TerminalCellWidth.Enumerate(hardLine.Text))
        {
            if (element.IsWhitespace)
            {
                AddWord(words, current);
                continue;
            }

            int absoluteStart = hardLine.StartOffset + element.SourceStartOffset;
            VisualTextStyle style = GetStyle(styles, absoluteStart, element.SourceLength);
            current.Add(new StyledLayoutTextElement(
                element.Text,
                element.Width,
                element.SourceStartOffset,
                element.SourceLength,
                style));
        }

        AddWord(words, current);
        return words;
    }

    private static void AddWord(List<FlowWord> words, List<StyledLayoutTextElement> current)
    {
        if (current.Count == 0)
        {
            return;
        }

        words.Add(new FlowWord(current.ToArray(), current[0].SourceStartOffset));
        current.Clear();
    }

    private static VisualTextStyle GetStyle(VisualTextStyle[] styles, int start, int length)
    {
        VisualTextStyle result = VisualTextStyle.None;
        int end = Math.Min(start + length, styles.Length);
        for (int index = start; index < end; index++)
        {
            result |= styles[index];
        }

        return result;
    }

    private static void AppendStyled(
        StringBuilder target,
        List<VisualTextSpan> spans,
        StyledLayoutTextElement[] elements)
    {
        foreach (StyledLayoutTextElement element in elements)
        {
            AppendStyled(target, spans, element.Text, element.Style);
        }
    }

    private static void AppendStyled(
        StringBuilder target,
        List<VisualTextSpan> spans,
        string text,
        VisualTextStyle style)
    {
        int start = target.Length;
        target.Append(text);

        if (style == VisualTextStyle.None || text.Length == 0)
        {
            return;
        }

        if (spans.Count > 0)
        {
            VisualTextSpan previous = spans[^1];
            if (previous.EndIndex == start && previous.Style == style)
            {
                spans[^1] = new VisualTextSpan(previous.StartIndex, previous.Length + text.Length, style);
                return;
            }
        }

        spans.Add(new VisualTextSpan(start, text.Length, style));
    }

    private static void AddWrappedSegment(
        List<WrappedSegment> lines,
        StringBuilder text,
        List<VisualTextSpan> spans,
        int sourceStartOffset,
        int sourceEndOffset)
    {
        lines.Add(new WrappedSegment(text.ToString(), sourceStartOffset, sourceEndOffset, spans.ToArray()));
        text.Clear();
        spans.Clear();
    }

    private static VisualTextSpan[] ShiftSpans(VisualTextSpan[] spans, int offset)
    {
        if (spans.Length == 0 || offset == 0)
        {
            return spans;
        }

        VisualTextSpan[] shifted = new VisualTextSpan[spans.Length];
        for (int index = 0; index < spans.Length; index++)
        {
            VisualTextSpan span = spans[index];
            shifted[index] = new VisualTextSpan(span.StartIndex + offset, span.Length, span.Style);
        }

        return shifted;
    }

    private static List<DisplayElement> ExpandTabs(string value)
    {
        List<DisplayElement> result = [];
        int column = 0;
        foreach (LayoutTextElement element in TerminalCellWidth.Enumerate(value))
        {
            if (element.Text == "\t")
            {
                int spaces = TabSize - (column % TabSize);
                for (int index = 0; index < spaces; index++)
                {
                    result.Add(new DisplayElement(" ", 1, element.SourceStartOffset));
                }

                column += spaces;
            }
            else
            {
                result.Add(new DisplayElement(element.Text, element.Width, element.SourceStartOffset));
                column += element.Width;
            }
        }

        return result;
    }

    private static IEnumerable<HardLine> EnumerateHardLines(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        int start = 0;
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (current is not ('\r' or '\n'))
            {
                continue;
            }

            int separatorLength = current == '\r' && index + 1 < value.Length && value[index + 1] == '\n' ? 2 : 1;
            yield return new HardLine(value[start..index], start, index + separatorLength);
            index += separatorLength - 1;
            start = index + 1;
        }

        yield return new HardLine(value[start..], start, value.Length);
    }

    private static string FormatImage(ImageBlock image)
    {
        string description = image.AlternativeText switch
        {
            string alternativeText when image.Caption is string caption => $"{alternativeText} — {caption}",
            string alternativeText => alternativeText,
            null when image.Caption is string caption => caption,
            _ => string.Empty,
        };

        return description.Length == 0 ? "[Immagine]" : $"[Immagine: {description}]";
    }

    private static void AddSpacing(List<VisualLine> target, SectionId? sectionId, BlockId? blockId)
    {
        if (target.Count == 0 || target[^1].Kind == VisualLineKind.Spacing)
        {
            return;
        }

        target.Add(new VisualLine(string.Empty, 0, VisualLineKind.Spacing, sectionId, blockId));
    }

    private static void AddLine(
        List<VisualLine> target,
        string text,
        VisualLineKind kind,
        SectionId sectionId,
        BlockId blockId,
        int sourceStartOffset,
        int sourceEndOffset,
        int? headingLevel = null,
        VisualTextSpan[]? styleSpans = null)
    {
        target.Add(
            new VisualLine(
                text,
                TerminalCellWidth.Measure(text),
                kind,
                sectionId,
                blockId,
                sourceStartOffset,
                sourceEndOffset,
                headingLevel,
                styleSpans));
    }

    private static System.Collections.ObjectModel.ReadOnlyCollection<LayoutPage> Paginate(
        IReadOnlyList<VisualLine> lines,
        int pageHeight)
    {
        List<LayoutPage> pages = [];
        List<VisualLine> current = [];
        foreach (VisualLine line in lines)
        {
            if (current.Count == pageHeight)
            {
                pages.Add(new LayoutPage(pages.Count + 1, current.ToArray()));
                current.Clear();
            }

            if (current.Count == 0 && line.Kind == VisualLineKind.Spacing)
            {
                continue;
            }

            current.Add(line);
        }

        if (current.Count > 0 || pages.Count == 0)
        {
            pages.Add(new LayoutPage(pages.Count + 1, current.ToArray()));
        }

        return pages.AsReadOnly();
    }

    private readonly record struct HardLine(string Text, int StartOffset, int SeparatorEndOffset);

    private readonly record struct WrappedSegment(string Text, int SourceStartOffset, int SourceEndOffset, VisualTextSpan[] StyleSpans);

    private sealed record FlowWord(StyledLayoutTextElement[] Elements, int SourceStartOffset);

    private readonly record struct StyledLayoutTextElement(
        string Text,
        int Width,
        int SourceStartOffset,
        int SourceLength,
        VisualTextStyle Style);

    private readonly record struct DisplayElement(string Text, int Width, int SourceStartOffset);
}
