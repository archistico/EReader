using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Content;

public sealed class QuoteBlock : ContentBlock
{
    public QuoteBlock(BlockId id, IEnumerable<InlineContent> content, int depth = 1)
        : base(id)
    {
        if (depth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), depth, "La profondità della citazione deve essere positiva.");
        }

        Content = DomainGuard.Freeze(content, nameof(content));
        Depth = depth;
    }

    public IReadOnlyList<InlineContent> Content { get; }

    public int Depth { get; }
}
