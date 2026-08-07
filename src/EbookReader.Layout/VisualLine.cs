using System.Collections.ObjectModel;
using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Layout;

/// <summary>
/// One visual line produced for a specific viewport, optionally mapped back to a logical Domain range.
/// </summary>
public sealed class VisualLine
{
    internal VisualLine(
        string text,
        int displayWidth,
        VisualLineKind kind,
        SectionId? sectionId,
        BlockId? blockId,
        int? sourceStartOffset = null,
        int? sourceEndOffset = null,
        int? headingLevel = null,
        VisualTextSpan[]? styleSpans = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(displayWidth);

        if (headingLevel is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(headingLevel));
        }

        if ((sourceStartOffset is null) != (sourceEndOffset is null))
        {
            throw new ArgumentException("Gli offset sorgente devono essere entrambi presenti o entrambi assenti.", nameof(sourceEndOffset));
        }

        if (sourceStartOffset is int start)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(start);
            ArgumentOutOfRangeException.ThrowIfLessThan(sourceEndOffset!.Value, start);
            if (sectionId is null || blockId is null)
            {
                throw new ArgumentException("Una riga con mapping sorgente deve avere SectionId e BlockId.", nameof(sectionId));
            }
        }

        VisualTextSpan[] spans = styleSpans ?? [];
        int previousEnd = 0;
        foreach (VisualTextSpan span in spans)
        {
            ArgumentNullException.ThrowIfNull(span);
            if (span.StartIndex < previousEnd || span.EndIndex > text.Length)
            {
                throw new ArgumentException("Gli style span devono essere ordinati, non sovrapposti e contenuti nella riga.", nameof(styleSpans));
            }

            previousEnd = span.EndIndex;
        }

        Text = text;
        DisplayWidth = displayWidth;
        Kind = kind;
        SectionId = sectionId;
        BlockId = blockId;
        SourceStartOffset = sourceStartOffset;
        SourceEndOffset = sourceEndOffset;
        HeadingLevel = headingLevel;
        StyleSpans = new ReadOnlyCollection<VisualTextSpan>(spans.ToArray());
    }

    public string Text { get; }

    public int DisplayWidth { get; }

    public VisualLineKind Kind { get; }

    public SectionId? SectionId { get; }

    public BlockId? BlockId { get; }

    /// <summary>
    /// Inclusive UTF-16 offset in the logical plain text of the source block.
    /// Null for synthetic spacing lines.
    /// </summary>
    public int? SourceStartOffset { get; }

    /// <summary>
    /// Exclusive UTF-16 offset in the logical plain text of the source block.
    /// Null for synthetic spacing lines.
    /// </summary>
    public int? SourceEndOffset { get; }

    public int? HeadingLevel { get; }

    public ReadOnlyCollection<VisualTextSpan> StyleSpans { get; }

    public ReadingLocation? StartLocation =>
        SectionId is SectionId sectionId
        && BlockId is BlockId blockId
        && SourceStartOffset is int offset
            ? new ReadingLocation(sectionId, blockId, offset)
            : null;
}
