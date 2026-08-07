namespace EbookReader.Domain.Content;

public sealed class EmphasisSpan : InlineContainer
{
    public EmphasisSpan(IEnumerable<InlineContent> content)
        : base(content)
    {
    }
}
