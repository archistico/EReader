namespace EbookReader.Domain.Content;

public sealed class LineBreakInline : InlineContent
{
    private LineBreakInline()
    {
    }

    public static LineBreakInline Instance { get; } = new();
}
