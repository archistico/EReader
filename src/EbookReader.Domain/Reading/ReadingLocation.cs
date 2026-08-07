using EbookReader.Domain.Books;
using EbookReader.Domain.Content;

namespace EbookReader.Domain.Reading;

public sealed record ReadingLocation
{
    public ReadingLocation(SectionId sectionId, BlockId? blockId = null, int characterOffset = 0)
    {
        ArgumentNullException.ThrowIfNull(sectionId);
        if (characterOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(characterOffset), characterOffset, "L'offset non può essere negativo.");
        }

        if (blockId is null && characterOffset != 0)
        {
            throw new ArgumentException("Una posizione di sezione senza BlockId deve avere offset zero.", nameof(characterOffset));
        }

        SectionId = sectionId;
        BlockId = blockId;
        CharacterOffset = characterOffset;
    }

    public SectionId SectionId { get; }

    public BlockId? BlockId { get; }

    public int CharacterOffset { get; }

    public static ReadingLocation AtSectionStart(SectionId sectionId) => new(sectionId);

    public static ReadingLocation AtBlockStart(SectionId sectionId, BlockId blockId) => new(sectionId, blockId);
}
