using EbookReader.Domain.Internal;
using EbookReader.Domain.Resources;

namespace EbookReader.Domain.Content;

public sealed class ImageBlock : ContentBlock
{
    public ImageBlock(BlockId id, ResourceId resourceId, string? alternativeText = null, string? caption = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(resourceId);
        ResourceId = resourceId;
        AlternativeText = DomainGuard.OptionalText(alternativeText);
        Caption = DomainGuard.OptionalText(caption);
    }

    public ResourceId ResourceId { get; }

    public string? AlternativeText { get; }

    public string? Caption { get; }
}
