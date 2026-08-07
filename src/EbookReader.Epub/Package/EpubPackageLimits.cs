namespace EbookReader.Epub.Package;

internal static class EpubPackageLimits
{
    public const int MaxPackageDocumentBytes = 4 * 1024 * 1024;
    public const int MaxManifestItems = 20_000;
    public const int MaxSpineItems = 20_000;
    public const int MaxMetadataEntries = 10_000;
}
