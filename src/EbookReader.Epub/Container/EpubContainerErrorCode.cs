namespace EbookReader.Epub.Container;

/// <summary>
/// Stable machine-readable categories for failures detected while opening the OCF container.
/// </summary>
public enum EpubContainerErrorCode
{
    InvalidContainer = 0,
    InvalidZip = 1,
    UnsupportedZipFeature = 2,
    TooManyEntries = 3,
    MissingMimeType = 4,
    MimeTypeNotFirst = 5,
    MimeTypeCompressed = 6,
    MimeTypeHasExtraField = 7,
    InvalidMimeTypeContent = 8,
    InvalidContainerPath = 9,
    DuplicateContainerEntry = 10,
    MissingContainerXml = 11,
    ContainerXmlTooLarge = 12,
    InvalidContainerXml = 13,
    InvalidContainerVersion = 14,
    MissingRootfiles = 15,
    TooManyRootfiles = 16,
    InvalidRootfile = 17,
    InvalidRootfileMediaType = 18,
    RootfileNotFound = 19,
    EntryNotFound = 20,
}
