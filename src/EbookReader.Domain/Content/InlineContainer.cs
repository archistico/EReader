using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Content;

public abstract class InlineContainer : InlineContent
{
    protected InlineContainer(IEnumerable<InlineContent> content)
    {
        Content = DomainGuard.Freeze(content, nameof(content));
    }

    public IReadOnlyList<InlineContent> Content { get; }
}
