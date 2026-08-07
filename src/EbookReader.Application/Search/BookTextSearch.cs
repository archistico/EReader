using EbookReader.Domain.Books;
using EbookReader.Domain.Content;
using EbookReader.Domain.Reading;

namespace EbookReader.Application.Search;

/// <summary>
/// Deterministic pre-layout search over the logical plain-text projection of Domain content blocks.
/// Search never inspects paginated or terminal-wrapped projections.
/// </summary>
public static class BookTextSearch
{
    public const int MaximumQueryLength = 256;
    public const int MaximumMatches = 10_000;

    public static BookSearchResultSet Search(Book book, string query)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        string normalizedQuery = query.Trim();
        if (normalizedQuery.Length > MaximumQueryLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                query.Length,
                $"La ricerca non può superare {MaximumQueryLength} code unit UTF-16.");
        }

        List<BookSearchMatch> matches = [];

        foreach (ReadingSection section in book.ReadingOrder)
        {
            foreach (ContentBlock block in section.Blocks)
            {
                string text = ContentText.GetPlainText(block);
                if (text.Length == 0)
                {
                    continue;
                }

                int searchOffset = 0;
                while (searchOffset <= text.Length - normalizedQuery.Length)
                {
                    int matchOffset = text.IndexOf(
                        normalizedQuery,
                        searchOffset,
                        StringComparison.OrdinalIgnoreCase);
                    if (matchOffset < 0)
                    {
                        break;
                    }

                    matches.Add(new BookSearchMatch(
                        new ReadingLocation(section.Id, block.Id, matchOffset),
                        normalizedQuery.Length));

                    if (matches.Count >= MaximumMatches)
                    {
                        return new BookSearchResultSet(normalizedQuery, matches, isTruncated: true);
                    }

                    // Advance one UTF-16 code unit so overlapping matches remain discoverable.
                    searchOffset = matchOffset + 1;
                }
            }
        }

        return new BookSearchResultSet(normalizedQuery, matches, isTruncated: false);
    }
}
