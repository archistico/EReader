using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Books;

public sealed class BookMetadata
{
    public BookMetadata(
        string title,
        string? subtitle = null,
        IEnumerable<string>? languages = null,
        IEnumerable<BookContributor>? contributors = null,
        IEnumerable<BookIdentifier>? identifiers = null,
        string? description = null,
        string? publisher = null,
        IEnumerable<string>? subjects = null,
        string? rights = null)
    {
        Title = DomainGuard.RequiredText(title, nameof(title));
        Subtitle = DomainGuard.OptionalText(subtitle);
        Languages = FreezeRequiredTexts(languages, nameof(languages));
        Contributors = DomainGuard.Freeze(contributors ?? Array.Empty<BookContributor>(), nameof(contributors));
        Identifiers = DomainGuard.Freeze(identifiers ?? Array.Empty<BookIdentifier>(), nameof(identifiers));
        Description = DomainGuard.OptionalText(description);
        Publisher = DomainGuard.OptionalText(publisher);
        Subjects = FreezeRequiredTexts(subjects, nameof(subjects));
        Rights = DomainGuard.OptionalText(rights);
    }

    public string Title { get; }

    public string? Subtitle { get; }

    public IReadOnlyList<string> Languages { get; }

    public IReadOnlyList<BookContributor> Contributors { get; }

    public IReadOnlyList<BookIdentifier> Identifiers { get; }

    public string? Description { get; }

    public string? Publisher { get; }

    public IReadOnlyList<string> Subjects { get; }

    public string? Rights { get; }

    private static IReadOnlyList<string> FreezeRequiredTexts(IEnumerable<string>? values, string parameterName)
    {
        if (values is null)
        {
            return Array.Empty<string>();
        }

        string[] normalized = values
            .Select(value => DomainGuard.RequiredText(value, parameterName))
            .ToArray();

        return Array.AsReadOnly(normalized);
    }
}
