namespace EbookReader.Domain.Content;

public abstract class ContentBlock
{
    protected ContentBlock(BlockId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    public BlockId Id { get; }
}
