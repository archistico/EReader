namespace EbookReader.Epub.Validation;

internal static class EpubProtectionLimits
{
    public const int MaxEncryptionDocumentBytes = 1024 * 1024;
    public const int MaxProtectedResources = 10_000;
}
