namespace EbookReader.Epub.Validation;

/// <summary>
/// Stable diagnostic identifiers that are not direct projections of parser error-code enums.
/// </summary>
public static class EpubDiagnosticCodes
{
    public const string RightsManagementMetadataPresent = "ER-EPUB-PROTECTION-INFO-001";
    public const string FontObfuscationPresent = "ER-EPUB-PROTECTION-INFO-002";
    public const string UnsupportedEncryption = "ER-EPUB-PROTECTION-UNSUPPORTED-001";

    public const string NavigationUnavailable = "ER-EPUB-RECOVERY-NAVIGATION-001";
    public const string TableOfContentsDropped = "ER-EPUB-RECOVERY-NAVIGATION-002";
    public const string SupplementarySpineItemSkipped = "ER-EPUB-RECOVERY-CONTENT-001";
    public const string MissingReferencedImage = "ER-EPUB-RECOVERY-RESOURCE-001";
    public const string MissingOptionalResource = "ER-EPUB-RECOVERY-RESOURCE-002";
    public const string BrokenInternalHyperlink = "ER-EPUB-RECOVERY-LINK-001";
    public const string UnsafeExternalHyperlinkSuppressed = "ER-EPUB-SECURITY-LINK-001";
    public const string NavigationTargetDropped = "ER-EPUB-RECOVERY-NAVIGATION-003";
}
