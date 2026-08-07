namespace EbookReader.Epub.Navigation;

/// <summary>
/// Navigation list semantics understood by EReader at the EPUB adapter boundary.
/// </summary>
public enum EpubNavigationListKind
{
    TableOfContents = 0,
    PageList = 1,
    Landmarks = 2,
}
