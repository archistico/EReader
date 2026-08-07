namespace EbookReader.Epub.Navigation;

/// <summary>
/// Stable machine-readable categories for navigation failures.
/// </summary>
public enum EpubNavigationErrorCode
{
    InvalidNavigation = 0,
    NavigationDocumentNotFound = 1,
    MultipleNavigationDocuments = 2,
    InvalidNavigationMediaType = 3,
    NavigationSourceMustBeLocal = 4,
    NavigationDocumentTooLarge = 5,
    InvalidNavigationXhtml = 6,
    InvalidNavigationXhtmlNamespace = 7,
    MissingTableOfContents = 8,
    DuplicateNavigationAid = 9,
    InvalidNavigationStructure = 10,
    EmptyNavigationLabel = 11,
    InvalidNavigationHref = 12,
    NavigationTargetNotFound = 13,
    TooManyNavigationNodes = 14,
    NavigationDepthExceeded = 15,
    MissingNcxReference = 16,
    NcxManifestItemNotFound = 17,
    InvalidNcxMediaType = 18,
    NcxDocumentTooLarge = 19,
    InvalidNcxXml = 20,
    InvalidNcxNamespace = 21,
    MissingNcxNavMap = 22,
    InvalidNcxNavPoint = 23,
    DuplicateNcxId = 24,
}
