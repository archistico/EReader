using System.Collections.ObjectModel;

namespace EbookReader.Epub.Navigation;

/// <summary>
/// One normalized navigation aid, such as the table of contents.
/// </summary>
public sealed class EpubNavigationList
{
    internal EpubNavigationList(
        EpubNavigationListKind kind,
        string? label,
        List<EpubNavigationNode> items)
    {
        Kind = kind;
        Label = label;
        Items = new ReadOnlyCollection<EpubNavigationNode>(items);
    }

    public EpubNavigationListKind Kind { get; }

    public string? Label { get; }

    public IReadOnlyList<EpubNavigationNode> Items { get; }
}
