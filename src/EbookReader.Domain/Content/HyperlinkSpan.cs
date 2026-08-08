namespace EbookReader.Domain.Content;

public sealed class HyperlinkSpan : InlineContainer
{
    public HyperlinkSpan(
        LinkTarget target,
        IEnumerable<InlineContent> content,
        HyperlinkRole role = HyperlinkRole.Generic)
        : base(content)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
        Role = role;
    }

    public LinkTarget Target { get; }

    public HyperlinkRole Role { get; }
}
