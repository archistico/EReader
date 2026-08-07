namespace EbookReader.Epub.Navigation;

/// <summary>
/// Physical EPUB mechanism from which the normalized navigation model was read.
/// </summary>
public enum EpubNavigationSourceKind
{
    Epub3NavigationDocument = 0,
    Epub2Ncx = 1,
}
