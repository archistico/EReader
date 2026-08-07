namespace EbookReader.Epub.Package;

/// <summary>
/// Stable machine-readable categories for Package Document failures.
/// </summary>
public enum EpubPackageErrorCode
{
    InvalidPackage = 0,
    PackageDocumentTooLarge = 1,
    InvalidPackageXml = 2,
    InvalidPackageNamespace = 3,
    UnsupportedPackageVersion = 4,
    MissingUniqueIdentifierAttribute = 5,
    MissingMetadata = 6,
    MissingIdentifier = 7,
    UniqueIdentifierNotFound = 8,
    MissingTitle = 9,
    MissingLanguage = 10,
    MissingModifiedMetadata = 11,
    InvalidModifiedMetadata = 12,
    MissingManifest = 13,
    EmptyManifest = 14,
    InvalidManifestItem = 15,
    DuplicateManifestId = 16,
    InvalidManifestHref = 17,
    DuplicateManifestResource = 18,
    ManifestResourceNotFound = 19,
    PackageDocumentSelfReference = 20,
    InvalidFallbackReference = 21,
    CircularFallback = 22,
    InvalidMediaOverlayReference = 23,
    MissingSpine = 24,
    EmptySpine = 25,
    InvalidSpineItem = 26,
    SpineManifestItemNotFound = 27,
    NoLinearSpineItem = 28,
    InvalidPageProgressionDirection = 29,
}
