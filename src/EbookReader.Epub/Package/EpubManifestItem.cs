using System.Collections.ObjectModel;
using EbookReader.Epub.Container;

namespace EbookReader.Epub.Package;

/// <summary>
/// One OPF manifest item. A resource is either local to OCF or represented by an absolute URL.
/// </summary>
public sealed class EpubManifestItem
{
    internal EpubManifestItem(
        string id,
        string href,
        string mediaType,
        OcfPath? localPath,
        Uri? remoteUri,
        HashSet<string> properties,
        string? fallbackId,
        string? mediaOverlayId)
    {
        Id = id;
        Href = href;
        MediaType = mediaType;
        LocalPath = localPath;
        RemoteUri = remoteUri;
        Properties = new ReadOnlyCollection<string>(properties.Order(StringComparer.Ordinal).ToArray());
        FallbackId = fallbackId;
        MediaOverlayId = mediaOverlayId;
    }

    public string Id { get; }

    public string Href { get; }

    public string MediaType { get; }

    public OcfPath? LocalPath { get; }

    public Uri? RemoteUri { get; }

    public IReadOnlyList<string> Properties { get; }

    public string? FallbackId { get; }

    public string? MediaOverlayId { get; }

    public bool IsRemote => RemoteUri is not null;

    public bool HasProperty(string property)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        return Properties.Contains(property);
    }
}
