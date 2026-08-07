using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Content;

public sealed class ParagraphBlock : ContentBlock
{
    public ParagraphBlock(BlockId id, IEnumerable<InlineContent>? content = null)
        : base(id)
    {
        Content = DomainGuard.Freeze(content ?? Array.Empty<InlineContent>(), nameof(content));
    }

    public IReadOnlyList<InlineContent> Content { get; }
}
