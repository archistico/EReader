using EbookReader.Domain.Content;
using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Books;

public sealed class ReadingSection
{
    public ReadingSection(
        SectionId id,
        IEnumerable<ContentBlock> blocks,
        string? title = null,
        ReadingSectionRole role = ReadingSectionRole.Primary)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
        Title = DomainGuard.OptionalText(title);
        Role = DomainGuard.DefinedEnum(role, nameof(role));
        Blocks = DomainGuard.Freeze(blocks, nameof(blocks));

        BlockId[] duplicateIds = Blocks
            .GroupBy(block => block.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateIds.Length > 0)
        {
            throw new ArgumentException(
                $"La sezione '{Id}' contiene BlockId duplicati: {string.Join(", ", duplicateIds)}.",
                nameof(blocks));
        }
    }

    public SectionId Id { get; }

    public string? Title { get; }

    public ReadingSectionRole Role { get; }

    public IReadOnlyList<ContentBlock> Blocks { get; }

    public ContentBlock? FindBlock(BlockId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return Blocks.FirstOrDefault(block => block.Id == id);
    }
}
