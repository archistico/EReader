namespace EbookReader.Epub.Validation;

/// <summary>
/// Stable machine-readable categories for malformed protection metadata.
/// </summary>
public enum EpubProtectionErrorCode
{
    InvalidProtectionDocument = 0,
    ProtectionDocumentTooLarge = 1,
    InvalidProtectionXml = 2,
    InvalidProtectionNamespace = 3,
    MissingEncryptedData = 4,
    InvalidEncryptedData = 5,
    InvalidCipherReference = 6,
    ProtectedResourceNotFound = 7,
    ForbiddenProtectedResource = 8,
    DuplicateProtectedResource = 9,
    FontObfuscationResourceNotInManifest = 10,
    FontObfuscationTargetNotFont = 11,
}
