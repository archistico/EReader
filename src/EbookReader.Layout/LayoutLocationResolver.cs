using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Layout;

/// <summary>
/// Maps stable logical ReadingLocation values to ephemeral coordinates of one deterministic layout.
/// </summary>
public static class LayoutLocationResolver
{
    public static LayoutPosition Locate(Book book, BookLayout layout, ReadingLocation location)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(location);

        if (!book.ContainsLocation(location))
        {
            throw new ArgumentOutOfRangeException(nameof(location), "La ReadingLocation non appartiene al libro.");
        }

        if (location.BlockId is not BlockId blockId)
        {
            return LocateSectionStart(book, layout, location.SectionId);
        }

        List<(LayoutPosition Position, VisualLine Line)> blockLines = EnumerateMappedLines(layout)
            .Where(item => item.Line.SectionId == location.SectionId && item.Line.BlockId == blockId)
            .ToList();

        if (blockLines.Count == 0)
        {
            return LocateEmptyBlockFallback(book, layout, location);
        }

        int blockLength = GetBlockLength(book, location.SectionId, blockId);
        if (location.CharacterOffset == blockLength)
        {
            return blockLines[^1].Position;
        }

        foreach ((LayoutPosition position, VisualLine line) in blockLines)
        {
            if (line.SourceStartOffset is not int start || line.SourceEndOffset is not int end)
            {
                continue;
            }

            if (location.CharacterOffset >= start && location.CharacterOffset < end)
            {
                return position;
            }
        }

        foreach ((LayoutPosition position, VisualLine line) in blockLines)
        {
            if (line.SourceEndOffset is int end && location.CharacterOffset <= end)
            {
                return position;
            }
        }

        return blockLines[^1].Position;
    }

    public static ReadingLocation GetLineStart(BookLayout layout, LayoutPosition position)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(position);

        VisualLine line = GetLine(layout, position);
        if (line.StartLocation is ReadingLocation exact)
        {
            return exact;
        }

        List<(LayoutPosition Position, VisualLine Line)> mapped = EnumerateMappedLines(layout).ToList();
        int current = ToAbsoluteLineIndex(layout, position);

        foreach ((LayoutPosition candidatePosition, VisualLine candidateLine) in mapped)
        {
            if (ToAbsoluteLineIndex(layout, candidatePosition) > current
                && candidateLine.StartLocation is ReadingLocation nextLocation)
            {
                return nextLocation;
            }
        }

        for (int index = mapped.Count - 1; index >= 0; index--)
        {
            (LayoutPosition candidatePosition, VisualLine candidateLine) = mapped[index];
            if (ToAbsoluteLineIndex(layout, candidatePosition) < current
                && candidateLine.StartLocation is ReadingLocation previousLocation)
            {
                return previousLocation;
            }
        }

        throw new InvalidOperationException("Il layout non contiene alcuna riga associata a una ReadingLocation.");
    }

    internal static IEnumerable<(LayoutPosition Position, VisualLine Line)> EnumerateMappedLines(BookLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        foreach (LayoutPage page in layout.Pages)
        {
            for (int lineIndex = 0; lineIndex < page.Lines.Count; lineIndex++)
            {
                VisualLine line = page.Lines[lineIndex];
                if (line.StartLocation is not null)
                {
                    yield return (new LayoutPosition(page.Number, lineIndex), line);
                }
            }
        }
    }

    internal static VisualLine GetLine(BookLayout layout, LayoutPosition position)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(position);

        if (position.PageNumber > layout.Pages.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "La pagina non esiste nel layout.");
        }

        LayoutPage page = layout.Pages[position.PageNumber - 1];
        if (position.LineIndex >= page.Lines.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "La riga non esiste nella pagina.");
        }

        return page.Lines[position.LineIndex];
    }

    internal static int ToAbsoluteLineIndex(BookLayout layout, LayoutPosition position)
    {
        GetLine(layout, position);
        int index = position.LineIndex;
        for (int pageIndex = 0; pageIndex < position.PageNumber - 1; pageIndex++)
        {
            index += layout.Pages[pageIndex].Lines.Count;
        }

        return index;
    }

    private static LayoutPosition LocateSectionStart(Book book, BookLayout layout, SectionId sectionId)
    {
        foreach ((LayoutPosition position, VisualLine line) in EnumerateMappedLines(layout))
        {
            if (line.SectionId == sectionId)
            {
                return position;
            }
        }

        int sectionIndex = book.ReadingOrder
            .Select((section, index) => (section, index))
            .Single(item => item.section.Id == sectionId)
            .index;
        HashSet<SectionId> laterIds = book.ReadingOrder.Skip(sectionIndex + 1).Select(section => section.Id).ToHashSet();
        foreach ((LayoutPosition position, VisualLine line) in EnumerateMappedLines(layout))
        {
            if (line.SectionId is SectionId candidate && laterIds.Contains(candidate))
            {
                return position;
            }
        }

        HashSet<SectionId> earlierIds = book.ReadingOrder.Take(sectionIndex).Select(section => section.Id).ToHashSet();
        List<(LayoutPosition Position, VisualLine Line)> mapped = EnumerateMappedLines(layout).ToList();
        for (int index = mapped.Count - 1; index >= 0; index--)
        {
            if (mapped[index].Line.SectionId is SectionId candidate && earlierIds.Contains(candidate))
            {
                return mapped[index].Position;
            }
        }

        throw new InvalidOperationException($"La sezione '{sectionId}' non ha una proiezione visuale.");
    }

    private static LayoutPosition LocateEmptyBlockFallback(Book book, BookLayout layout, ReadingLocation location)
    {
        ReadingSection section = book.FindSection(location.SectionId)
            ?? throw new InvalidOperationException("La sezione della ReadingLocation non esiste.");
        int blockIndex = section.Blocks
            .Select((block, index) => (block, index))
            .Single(item => item.block.Id == location.BlockId)
            .index;

        HashSet<BlockId> laterIds = section.Blocks.Skip(blockIndex + 1).Select(block => block.Id).ToHashSet();
        foreach ((LayoutPosition position, VisualLine line) in EnumerateMappedLines(layout))
        {
            if (line.SectionId == section.Id
                && line.BlockId is BlockId candidate
                && laterIds.Contains(candidate))
            {
                return position;
            }
        }

        HashSet<BlockId> earlierIds = section.Blocks.Take(blockIndex).Select(block => block.Id).ToHashSet();
        List<(LayoutPosition Position, VisualLine Line)> mapped = EnumerateMappedLines(layout).ToList();
        for (int index = mapped.Count - 1; index >= 0; index--)
        {
            VisualLine line = mapped[index].Line;
            if (line.SectionId == section.Id
                && line.BlockId is BlockId candidate
                && earlierIds.Contains(candidate))
            {
                return mapped[index].Position;
            }
        }

        return LocateSectionStart(book, layout, section.Id);
    }

    private static int GetBlockLength(Book book, SectionId sectionId, BlockId blockId)
    {
        ReadingSection section = book.FindSection(sectionId)
            ?? throw new InvalidOperationException("La sezione non esiste.");
        ContentBlock block = section.FindBlock(blockId)
            ?? throw new InvalidOperationException("Il blocco non esiste.");
        return ContentText.GetPlainText(block).Length;
    }
}
