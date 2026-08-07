using System.Collections.ObjectModel;

namespace EbookReader.Application.Search;

/// <summary>
/// Bounded result set for one logical-text query.
/// </summary>
public sealed class BookSearchResultSet
{
    public BookSearchResultSet(string query, IEnumerable<BookSearchMatch> matches, bool isTruncated)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(matches);

        Query = query;
        Matches = matches.ToList().AsReadOnly();
        IsTruncated = isTruncated;
    }

    public string Query { get; }

    public ReadOnlyCollection<BookSearchMatch> Matches { get; }

    public bool IsTruncated { get; }
}
