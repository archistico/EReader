using System.Collections.ObjectModel;
using EbookReader.Epub.Container;

namespace EbookReader.Epub.Navigation;

/// <summary>
/// EPUB-specific but source-normalized navigation representation.
/// It deliberately does not expose XHTML or NCX DOM objects.
/// </summary>
public sealed class EpubNavigationDocument
{
    internal EpubNavigationDocument(
        EpubNavigationSourceKind sourceKind,
        OcfPath sourcePath,
        List<EpubNavigationList> lists)
    {
        SourceKind = sourceKind;
        SourcePath = sourcePath;
        Lists = new ReadOnlyCollection<EpubNavigationList>(lists);
    }

    public EpubNavigationSourceKind SourceKind { get; }

    public OcfPath SourcePath { get; }

    public IReadOnlyList<EpubNavigationList> Lists { get; }

    public EpubNavigationList TableOfContents =>
        Lists.Single(list => list.Kind == EpubNavigationListKind.TableOfContents);

    public EpubNavigationList? PageList =>
        Lists.SingleOrDefault(list => list.Kind == EpubNavigationListKind.PageList);

    public EpubNavigationList? Landmarks =>
        Lists.SingleOrDefault(list => list.Kind == EpubNavigationListKind.Landmarks);
}
