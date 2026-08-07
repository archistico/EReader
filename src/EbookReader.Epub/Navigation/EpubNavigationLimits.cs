namespace EbookReader.Epub.Navigation;

internal static class EpubNavigationLimits
{
    public const int MaxDocumentBytes = 4 * 1024 * 1024;
    public const int MaxNodes = 20_000;
    public const int MaxDepth = 64;
    public const int MaxLabelLength = 16_384;
}
