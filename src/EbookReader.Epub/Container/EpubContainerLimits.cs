namespace EbookReader.Epub.Container;

internal static class EpubContainerLimits
{
    public const int MaxEntries = 100_000;
    public const int MaxRootfiles = 128;
    public const long MaxContainerXmlBytes = 1_048_576;
    public const long MaxMimeTypeBytes = 64;
}
