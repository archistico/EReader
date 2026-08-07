namespace EbookReader.Epub.Container;

/// <summary>
/// Describes one package document declared by META-INF/container.xml.
/// </summary>
public sealed record EpubRootFile
{
    public const string PackageDocumentMediaType = "application/oebps-package+xml";

    internal EpubRootFile(OcfPath path, string mediaType)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);

        Path = path;
        MediaType = mediaType;
    }

    public OcfPath Path { get; }

    public string MediaType { get; }
}
