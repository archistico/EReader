using EbookReader.Domain.Resources;
using EbookReader.Epub.Container;
using EbookReader.Epub.Package;

namespace EbookReader.Epub.Resources;

/// <summary>
/// Reads one explicitly requested local raster image from an EPUB into bounded memory.
/// No archive extraction, network retrieval, SVG execution or DRM handling is performed here.
/// </summary>
public static class EpubImageResourceReader
{
    public const int MaximumImageBytes = 16 * 1024 * 1024;

    public static EpubImageResource Read(string epubFilePath, ResourceId resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(epubFilePath);
        ArgumentNullException.ThrowIfNull(resourceId);

        using EpubContainer container = EpubContainer.Open(epubFilePath);
        EpubPackageDocument package = EpubPackageReader.ReadForRecovery(container);
        EpubManifestItem? item = package.Manifest.SingleOrDefault(candidate =>
            string.Equals(candidate.Id, resourceId.Value, StringComparison.Ordinal));

        if (item is null)
        {
            throw Error(
                EpubImageResourceErrorCode.ResourceNotFound,
                $"La risorsa immagine '{resourceId}' non esiste nel manifest EPUB.");
        }

        OcfPath localPath = item.LocalPath
            ?? throw Error(
                EpubImageResourceErrorCode.ResourceIsRemote,
                $"La risorsa '{resourceId}' è remota e non può essere aperta dal reader offline.");

        if (!item.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw Error(
                EpubImageResourceErrorCode.ResourceIsNotImage,
                $"La risorsa '{resourceId}' non è dichiarata come immagine nel manifest EPUB.");
        }

        if (!container.Contains(localPath))
        {
            throw Error(
                EpubImageResourceErrorCode.ResourceNotFound,
                $"La risorsa immagine '{resourceId}' è dichiarata nel manifest ma manca dal contenitore EPUB.");
        }

        string extension = GetSafeRasterExtension(item.MediaType);
        using Stream source = container.OpenEntry(localPath);
        byte[] data = ReadBounded(source, resourceId);
        return new EpubImageResource(resourceId, item.MediaType, extension, data);
    }

    private static byte[] ReadBounded(Stream source, ResourceId resourceId)
    {
        using MemoryStream buffer = new();
        byte[] chunk = new byte[16 * 1024];
        int total = 0;

        while (true)
        {
            int read = source.Read(chunk.AsSpan());
            if (read == 0)
            {
                return buffer.ToArray();
            }

            total += read;
            if (total > MaximumImageBytes)
            {
                throw Error(
                    EpubImageResourceErrorCode.ResourceTooLarge,
                    $"La risorsa immagine '{resourceId}' supera il limite di {MaximumImageBytes} byte per l'anteprima esterna.");
            }

            buffer.Write(chunk.AsSpan(0, read));
        }
    }

    private static string GetSafeRasterExtension(string mediaType)
    {
        if (string.Equals(mediaType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return ".jpg";
        }

        if (string.Equals(mediaType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            return ".png";
        }

        if (string.Equals(mediaType, "image/gif", StringComparison.OrdinalIgnoreCase))
        {
            return ".gif";
        }

        if (string.Equals(mediaType, "image/webp", StringComparison.OrdinalIgnoreCase))
        {
            return ".webp";
        }

        throw Error(
            EpubImageResourceErrorCode.UnsupportedImageMediaType,
            $"Il media type immagine '{mediaType}' non è supportato per l'anteprima esterna. SVG e altri formati restano rappresentati come placeholder testuale.");
    }

    private static EpubImageResourceException Error(EpubImageResourceErrorCode errorCode, string message) =>
        new(errorCode, message);
}
