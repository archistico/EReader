using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Books;

public sealed record BookContributor
{
    public BookContributor(string name, ContributorRole role, string? sortName = null)
    {
        Name = DomainGuard.RequiredText(name, nameof(name));
        Role = DomainGuard.DefinedEnum(role, nameof(role));
        SortName = DomainGuard.OptionalText(sortName);
    }

    public string Name { get; }

    public ContributorRole Role { get; }

    public string? SortName { get; }
}
