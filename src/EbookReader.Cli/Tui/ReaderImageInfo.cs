using EbookReader.Domain.Resources;

namespace EbookReader.Cli.Tui;

/// <summary>
/// Format-neutral image metadata projected from the current Domain ImageBlock and BookResource.
/// </summary>
public sealed record ReaderImageInfo(
    ResourceId ResourceId,
    string MediaType,
    string? AlternativeText,
    string? Caption,
    string? ResourceName);
