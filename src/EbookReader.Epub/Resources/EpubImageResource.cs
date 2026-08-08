using EbookReader.Domain.Resources;

namespace EbookReader.Epub.Resources;

/// <summary>
/// Bounded in-memory projection of one local raster image from the EPUB manifest.
/// The Domain resource descriptor deliberately remains payload-free.
/// </summary>
public sealed class EpubImageResource
{
    private readonly byte[] _data;

    internal EpubImageResource(ResourceId resourceId, string mediaType, string fileExtension, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(resourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);
        ArgumentNullException.ThrowIfNull(data);

        ResourceId = resourceId;
        MediaType = mediaType;
        FileExtension = fileExtension;
        _data = data;
    }

    public ResourceId ResourceId { get; }

    public string MediaType { get; }

    /// <summary>
    /// Safe extension derived exclusively from the manifest media type, including the leading dot.
    /// </summary>
    public string FileExtension { get; }

    public ReadOnlyMemory<byte> Data => _data;
}
