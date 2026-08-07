namespace EbookReader.Domain.Content;

public sealed class StrongSpan : InlineContainer
{
    public StrongSpan(IEnumerable<InlineContent> content)
        : base(content)
    {
    }
}
