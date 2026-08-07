using EbookReader.Domain.Internal;
using EbookReader.Domain.Reading;

namespace EbookReader.Domain.Navigation;

public sealed class NavigationItem
{
    public NavigationItem(
        string label,
        ReadingLocation? target,
        IEnumerable<NavigationItem>? children = null)
    {
        Label = DomainGuard.RequiredText(label, nameof(label));
        Target = target;
        Children = DomainGuard.Freeze(children ?? Array.Empty<NavigationItem>(), nameof(children));

        if (Target is null && Children.Count == 0)
        {
            throw new ArgumentException(
                "Una voce di navigazione senza target deve contenere almeno un figlio.",
                nameof(children));
        }
    }

    public string Label { get; }

    /// <summary>
    /// Destination when this node is directly navigable. Null represents a pure grouping node.
    /// </summary>
    public ReadingLocation? Target { get; }

    public IReadOnlyList<NavigationItem> Children { get; }
}
