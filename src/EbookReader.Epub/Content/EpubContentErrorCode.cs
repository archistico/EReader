namespace EbookReader.Epub.Content;

/// <summary>
/// Stable machine-readable categories for XHTML-to-Domain conversion failures.
/// </summary>
public enum EpubContentErrorCode
{
    InvalidContent = 0,
    ContentDocumentTooLarge = 1,
    UnsupportedSpineContent = 2,
    MissingSpineContent = 3,
    InvalidXhtml = 4,
    MissingBody = 5,
    TooManyContentNodes = 6,
    ContentDepthExceeded = 7,
    DuplicateAnchor = 8,
    InvalidLocalReference = 9,
    InternalTargetNotFound = 10,
    ImageResourceNotFound = 11,
    ImageResourceNotImage = 12,
    TooManyBlocks = 13,
}
