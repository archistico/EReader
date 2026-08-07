using EbookReader.Domain.Internal;

namespace EbookReader.Domain.Resources;

public sealed class BookResource
{
    public BookResource(
        ResourceId id,
        ResourceKind kind,
        string mediaType,
        string? name = null,
        long? byteLength = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (byteLength is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), byteLength, "La dimensione non può essere negativa.");
        }

        Id = id;
        Kind = DomainGuard.DefinedEnum(kind, nameof(kind));
        MediaType = DomainGuard.RequiredText(mediaType, nameof(mediaType));
        Name = DomainGuard.OptionalText(name);
        ByteLength = byteLength;
    }

    public ResourceId Id { get; }

    public ResourceKind Kind { get; }

    public string MediaType { get; }

    public string? Name { get; }

    public long? ByteLength { get; }
}
