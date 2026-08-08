namespace EbookReader.Epub.Container;

internal static class EpubContainerLimits
{
    public const int MaxEntries = 100_000;
    public const int MaxRootfiles = 128;
    public const long MaxContainerXmlBytes = 1_048_576;
    public const long MaxMimeTypeBytes = 64;

    // M3.9 security guardrails. These are intentionally far above the bounded
    // OPF/navigation/XHTML payloads consumed by the text reader, while still
    // rejecting pathological archive metadata before allocation/decompression.
    public const long MaxEntryUncompressedBytes = 256L * 1024 * 1024;
    public const long MaxTotalUncompressedBytes = 2L * 1024 * 1024 * 1024;
    public const long CompressionRatioInspectionThresholdBytes = 16L * 1024 * 1024;
    public const int MaxCompressionRatio = 500;
}
