namespace EbookReader.Domain.Content;

public sealed class HyperlinkSpan : InlineContainer
{
    public HyperlinkSpan(LinkTarget target, IEnumerable<InlineContent> content)
        : base(content)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
    }

    public LinkTarget Target { get; }
}
