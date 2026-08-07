using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Resources;

public sealed record ResourceId
{
    public ResourceId(string value)
    {
        Value = DomainGuard.RequiredText(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
