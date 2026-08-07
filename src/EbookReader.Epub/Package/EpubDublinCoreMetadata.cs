namespace EbookReader.Epub.Package;

/// <summary>
/// One Dublin Core metadata value as represented by the OPF package.
/// EPUB-specific refinements remain in the EPUB adapter and do not leak into Domain.
/// </summary>
public sealed record EpubDublinCoreMetadata(
    string Name,
    string Value,
    string? Id,
    string? Language,
    string? Role,
    string? FileAs,
    string? Scheme);
