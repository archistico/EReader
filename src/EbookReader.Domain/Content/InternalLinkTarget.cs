using EbookReader.Domain.Reading;

namespace EbookReader.Domain.Content;

public sealed class InternalLinkTarget : LinkTarget
{
    public InternalLinkTarget(ReadingLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        Location = location;
    }

    public ReadingLocation Location { get; }
}
