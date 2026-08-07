using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Content;

public sealed class HeadingBlock : ContentBlock
{
    public HeadingBlock(BlockId id, int level, IEnumerable<InlineContent> content)
        : base(id)
    {
        if (level < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Il livello heading deve essere positivo.");
        }

        Level = level;
        Content = DomainGuard.Freeze(content, nameof(content));
    }

    public int Level { get; }

    public IReadOnlyList<InlineContent> Content { get; }
}
