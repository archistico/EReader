using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Navigation;

public sealed class TableOfContents
{
    public TableOfContents(IEnumerable<NavigationItem>? items = null)
    {
        Items = DomainGuard.Freeze(items ?? Array.Empty<NavigationItem>(), nameof(items));
    }

    public IReadOnlyList<NavigationItem> Items { get; }

    public static TableOfContents Empty { get; } = new();
}
