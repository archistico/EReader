using System.Collections.ObjectModel;

namespace EbookReader.Epub.Package;

/// <summary>
/// EPUB Package metadata preserved before mapping to the format-neutral Domain model.
/// </summary>
public sealed class EpubPackageMetadata
{
    internal EpubPackageMetadata(
        List<EpubDublinCoreMetadata> dublinCore,
        string uniqueIdentifier,
        string? modified)
    {
        DublinCore = new ReadOnlyCollection<EpubDublinCoreMetadata>(dublinCore);
        UniqueIdentifier = uniqueIdentifier;
        Modified = modified;
    }

    public IReadOnlyList<EpubDublinCoreMetadata> DublinCore { get; }

    public string UniqueIdentifier { get; }

    public string? Modified { get; }

    public IReadOnlyList<string> Titles => Values("title");

    public IReadOnlyList<string> Languages => Values("language");

    public IReadOnlyList<string> Identifiers => Values("identifier");

    public IReadOnlyList<string> Creators => Values("creator");

    private ReadOnlyCollection<string> Values(string name) =>
        Array.AsReadOnly(
            DublinCore
                .Where(item => string.Equals(item.Name, name, StringComparison.Ordinal))
                .Select(item => item.Value)
                .ToArray());
}
