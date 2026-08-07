namespace EbookReader.Epub.Content;

internal static class EpubContentLimits
{
    public const int MaxContentDocumentBytes = 8 * 1024 * 1024;
    public const int MaxNodesPerDocument = 250_000;
    public const int MaxBlocksPerDocument = 50_000;
    public const int MaxTreeDepth = 64;
}
