namespace EbookReader.Domain.Internal;

internal static class DomainGuard
{
    public static string RequiredText(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        string normalized = value.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Il valore non può essere vuoto o composto solo da spazi.", parameterName);
        }

        return normalized;
    }

    public static string? OptionalText(string? value)
    {
        if (value is null)
        {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    public static T DefinedEnum<T>(T value, string parameterName)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Il valore enum non è definito.");
        }

        return value;
    }

    public static IReadOnlyList<T> Freeze<T>(IEnumerable<T> items, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(items, parameterName);
        T[] snapshot = items.ToArray();
        if (snapshot.Any(item => item is null))
        {
            throw new ArgumentException("La raccolta non può contenere elementi null.", parameterName);
        }

        return Array.AsReadOnly(snapshot);
    }
}
