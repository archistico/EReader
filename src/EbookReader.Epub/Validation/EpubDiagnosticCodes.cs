namespace EbookReader.Epub.Validation;

/// <summary>
/// Stable diagnostic identifiers that are not direct projections of parser error-code enums.
/// </summary>
public static class EpubDiagnosticCodes
{
    public const string RightsManagementMetadataPresent = "ER-EPUB-PROTECTION-INFO-001";
    public const string FontObfuscationPresent = "ER-EPUB-PROTECTION-INFO-002";
    public const string UnsupportedEncryption = "ER-EPUB-PROTECTION-UNSUPPORTED-001";
}
