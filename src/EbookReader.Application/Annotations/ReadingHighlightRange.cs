using EbookReader.Domain.Reading;

namespace EbookReader.Application.Annotations;

/// <summary>
/// Logical, layout-independent highlight range. M3.7 creates ranges within one Domain block;
/// Start is inclusive and End is exclusive in UTF-16 code units.
/// </summary>
public sealed record ReadingHighlightRange
{
    public ReadingHighlightRange(ReadingLocation start, ReadingLocation end)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        if (start.BlockId is null || end.BlockId is null)
        {
            throw new ArgumentException("Un'evidenziazione deve riferirsi a un blocco logico.", nameof(start));
        }

        if (start.SectionId != end.SectionId || start.BlockId != end.BlockId)
        {
            throw new ArgumentException("M3.7 supporta evidenziazioni contenute nello stesso blocco logico.", nameof(end));
        }

        if (end.CharacterOffset <= start.CharacterOffset)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "La fine dell'evidenziazione deve seguire l'inizio.");
        }

        Start = start;
        End = end;
    }

    public ReadingLocation Start { get; }

    public ReadingLocation End { get; }

    public bool Intersects(ReadingLocation start, ReadingLocation end)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        return Start.SectionId == start.SectionId
            && Start.BlockId == start.BlockId
            && Start.CharacterOffset < end.CharacterOffset
            && End.CharacterOffset > start.CharacterOffset;
    }
}
