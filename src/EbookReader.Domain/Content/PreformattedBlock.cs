namespace EbookReader.Domain.Content;

public sealed class PreformattedBlock : ContentBlock
{
    public PreformattedBlock(BlockId id, string text)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
    }

    public string Text { get; }
}
