using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Books;

public sealed record SectionId
{
    public SectionId(string value)
    {
        Value = DomainGuard.RequiredText(value, nameof(value));
    }

    public string Value { get; }

    public override string ToString() => Value;
}
