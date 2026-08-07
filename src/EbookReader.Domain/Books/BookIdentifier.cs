using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Books;

public sealed record BookIdentifier
{
    public BookIdentifier(string value, string? scheme = null)
    {
        Value = DomainGuard.RequiredText(value, nameof(value));
        Scheme = DomainGuard.OptionalText(scheme);
    }

    public string Value { get; }

    public string? Scheme { get; }
}
