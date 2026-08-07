using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace EbookReader.Application.Library;

public static class ReadingHistorySearch
{
    public const int MaximumQueryLength = 128;

    public static ReadOnlyCollection<ReadingHistoryEntry> Filter(
        IEnumerable<ReadingHistoryEntry> entries,
        string? query)
    {
        ArgumentNullException.ThrowIfNull(entries);

        ReadingHistoryEntry[] source = entries.ToArray();
        string normalizedQuery = NormalizeQuery(query);
        if (normalizedQuery.Length == 0)
        {
            return Array.AsReadOnly(source);
        }

        string[] tokens = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        List<ScoredEntry> matches = [];
        for (int index = 0; index < source.Length; index++)
        {
            ReadingHistoryEntry entry = source[index];
            int score = ScoreEntry(entry, tokens);
            if (score > 0)
            {
                matches.Add(new ScoredEntry(entry, score, index));
            }
        }

        ReadingHistoryEntry[] ordered = matches
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.SourceIndex)
            .Select(match => match.Entry)
            .ToArray();
        return Array.AsReadOnly(ordered);
    }

    private static string NormalizeQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        string trimmed = query.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, MaximumQueryLength, nameof(query));
        return Fold(trimmed);
    }

    private static int ScoreEntry(ReadingHistoryEntry entry, string[] tokens)
    {
        string title = Fold(entry.Title);
        string author = Fold(entry.AuthorLine ?? string.Empty);
        string fileName = Fold(Path.GetFileNameWithoutExtension(entry.BookPath));
        string path = Fold(entry.BookPath);
        int total = 0;

        foreach (string token in tokens)
        {
            int tokenScore = Math.Max(
                Math.Max(ScoreField(title, token, 500, allowFuzzy: true), ScoreField(author, token, 400, allowFuzzy: true)),
                Math.Max(ScoreField(fileName, token, 300, allowFuzzy: true), ScoreField(path, token, 150, allowFuzzy: false)));
            if (tokenScore == 0)
            {
                return 0;
            }

            total += tokenScore;
        }

        return total;
    }

    private static int ScoreField(string field, string token, int weight, bool allowFuzzy)
    {
        if (field.Length == 0)
        {
            return 0;
        }

        if (string.Equals(field, token, StringComparison.Ordinal))
        {
            return weight + 120;
        }

        if (field.StartsWith(token, StringComparison.Ordinal))
        {
            return weight + 100;
        }

        int containsIndex = field.IndexOf(token, StringComparison.Ordinal);
        if (containsIndex >= 0)
        {
            return weight + 80 - Math.Min(containsIndex, 40);
        }

        if (!allowFuzzy)
        {
            return 0;
        }

        int subsequencePenalty = ComputeSubsequencePenalty(field, token);
        return subsequencePenalty < 0 ? 0 : Math.Max(weight + 40 - Math.Min(subsequencePenalty, 80), 1);
    }

    private static int ComputeSubsequencePenalty(string field, string token)
    {
        int fieldIndex = 0;
        int previousMatch = -1;
        int penalty = 0;

        foreach (char expected in token)
        {
            while (fieldIndex < field.Length && field[fieldIndex] != expected)
            {
                fieldIndex++;
            }

            if (fieldIndex >= field.Length)
            {
                return -1;
            }

            if (previousMatch >= 0)
            {
                penalty += fieldIndex - previousMatch - 1;
            }
            else
            {
                penalty += fieldIndex;
            }

            previousMatch = fieldIndex;
            fieldIndex++;
        }

        return penalty + Math.Max(field.Length - token.Length, 0) / 8;
    }

    private static string Fold(string value)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        string decomposed = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(decomposed.Length);
        bool previousWasSpace = false;

        foreach (char character in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.EnclosingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }
                continue;
            }

            builder.Append(char.ToUpperInvariant(character));
            previousWasSpace = false;
        }

        return builder.ToString().TrimEnd();
    }

    private sealed record ScoredEntry(ReadingHistoryEntry Entry, int Score, int SourceIndex);
}
