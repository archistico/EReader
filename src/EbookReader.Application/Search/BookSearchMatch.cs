using EbookReader.Domain.Reading;

namespace EbookReader.Application.Search;

/// <summary>
/// One logical search match. Offsets and length are expressed in the Domain UTF-16 coordinate space.
/// </summary>
public sealed record BookSearchMatch
{
    public BookSearchMatch(ReadingLocation location, int matchLength)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(matchLength);

        Location = location;
        MatchLength = matchLength;
    }

    public ReadingLocation Location { get; }

    public int MatchLength { get; }
}
