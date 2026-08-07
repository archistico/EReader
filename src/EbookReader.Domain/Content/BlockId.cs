using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Content;

public sealed record BlockId
{
    public BlockId(string value)
    {
        Value = DomainGuard.RequiredText(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
