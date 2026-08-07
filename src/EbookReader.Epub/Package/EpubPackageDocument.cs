using System.Collections.ObjectModel;
using EbookReader.Epub.Container;

namespace EbookReader.Epub.Package;

/// <summary>
/// Parsed EPUB 2 or EPUB 3 OPF Package Document.
/// </summary>
public sealed class EpubPackageDocument
{
    internal EpubPackageDocument(
        OcfPath path,
        string version,
        string uniqueIdentifierId,
        EpubPackageMetadata metadata,
        List<EpubManifestItem> manifest,
        List<EpubSpineItem> spine,
        string? spineTocId,
        string? pageProgressionDirection)
    {
        Path = path;
        Version = version;
        UniqueIdentifierId = uniqueIdentifierId;
        Metadata = metadata;
        Manifest = new ReadOnlyCollection<EpubManifestItem>(manifest);
        Spine = new ReadOnlyCollection<EpubSpineItem>(spine);
        SpineTocId = spineTocId;
        PageProgressionDirection = pageProgressionDirection;
    }

    public OcfPath Path { get; }

    public string Version { get; }

    public bool IsEpub2 => string.Equals(Version, "2.0", StringComparison.Ordinal);

    public bool IsEpub3 => string.Equals(Version, "3.0", StringComparison.Ordinal);

    public string UniqueIdentifierId { get; }

    public EpubPackageMetadata Metadata { get; }

    public IReadOnlyList<EpubManifestItem> Manifest { get; }

    public IReadOnlyList<EpubSpineItem> Spine { get; }

    public string? SpineTocId { get; }

    public string? PageProgressionDirection { get; }

    public EpubManifestItem? NavigationDocument =>
        Manifest.FirstOrDefault(item => item.HasProperty("nav"));

    public EpubManifestItem GetManifestItem(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return Manifest.Single(item => string.Equals(item.Id, id, StringComparison.Ordinal));
    }
}
