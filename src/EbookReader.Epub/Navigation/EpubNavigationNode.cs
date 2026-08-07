using System.Collections.ObjectModel;

namespace EbookReader.Epub.Navigation;

/// <summary>
/// One node in a normalized EPUB navigation hierarchy.
/// A grouping node may have no target but must have children.
/// </summary>
public sealed class EpubNavigationNode
{
    internal EpubNavigationNode(
        string label,
        EpubNavigationTarget? target,
        HashSet<string> types,
        List<EpubNavigationNode> children)
    {
        Label = label;
        Target = target;
        Types = new ReadOnlyCollection<string>(types.Order(StringComparer.Ordinal).ToArray());
        Children = new ReadOnlyCollection<EpubNavigationNode>(children);
    }

    public string Label { get; }

    public EpubNavigationTarget? Target { get; }

    public IReadOnlyList<string> Types { get; }

    public IReadOnlyList<EpubNavigationNode> Children { get; }
}
