using System.Text;

namespace EbookReader.Epub.Container;

/// <summary>
/// Canonical, root-relative path inside the OCF virtual file system.
/// </summary>
public sealed record OcfPath
{
    private OcfPath(string value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>
    /// Creates a path from an actual ZIP entry name. ZIP entry names are file-system names,
    /// not URL strings, so percent escapes are not decoded here.
    /// </summary>
    public static OcfPath FromArchiveEntry(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);
        return CreateFromSegments(entryName, decodePercentEscapes: false, allowDotSegments: false);
    }

    /// <summary>
    /// Resolves a path-relative URL string used from META-INF against the container root.
    /// Percent escapes are decoded segment-by-segment and dot segments are normalized.
    /// </summary>
    public static OcfPath FromContainerReference(string reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return CreateFromSegments(reference, decodePercentEscapes: true, allowDotSegments: true);
    }

    public override string ToString() => Value;

    private static OcfPath CreateFromSegments(
        string input,
        bool decodePercentEscapes,
        bool allowDotSegments)
    {
        if (input.Length == 0)
        {
            throw InvalidPath("Il path OCF non può essere vuoto.");
        }

        if (input[0] == '/' || input.Contains('\\'))
        {
            throw InvalidPath($"Path OCF assoluto o con separatore non valido: '{input}'.");
        }

        if (input.Contains('#') || input.Contains('?'))
        {
            throw InvalidPath($"Il path OCF del rootfile non può contenere query o fragment: '{input}'.");
        }

        string[] rawSegments = input.Split('/');
        if (decodePercentEscapes && rawSegments[0].Contains(':'))
        {
            throw InvalidPath($"Il riferimento OCF non può contenere uno schema URL: '{input}'.");
        }

        List<string> normalized = new(rawSegments.Length);

        for (int index = 0; index < rawSegments.Length; index++)
        {
            string rawSegment = rawSegments[index];
            if (rawSegment.Length == 0)
            {
                throw InvalidPath($"Il path OCF contiene un segmento vuoto: '{input}'.");
            }

            string segment = decodePercentEscapes
                ? DecodeSegment(rawSegment, input)
                : rawSegment;

            if (segment is "." && allowDotSegments)
            {
                continue;
            }

            if (segment is ".." && allowDotSegments)
            {
                if (normalized.Count == 0)
                {
                    throw InvalidPath($"Il path OCF tenta di uscire dalla root del contenitore: '{input}'.");
                }

                normalized.RemoveAt(normalized.Count - 1);
                continue;
            }

            if (segment is "." or "..")
            {
                throw InvalidPath($"Il path ZIP contiene un segmento di traversal: '{input}'.");
            }

            if (segment.Length == 0)
            {
                throw InvalidPath($"Il path OCF contiene un nome file vuoto: '{input}'.");
            }

            if (segment.Any(char.IsControl))
            {
                throw InvalidPath($"Il path OCF contiene caratteri di controllo: '{input}'.");
            }

            normalized.Add(segment);
        }

        if (normalized.Count == 0)
        {
            throw InvalidPath($"Il path OCF non identifica un file: '{input}'.");
        }

        string value = string.Join("/", normalized);
        if (Encoding.UTF8.GetByteCount(value) > 65_535)
        {
            throw InvalidPath("Il path OCF supera il limite di 65535 byte UTF-8.");
        }

        return new OcfPath(value);
    }

    private static string DecodeSegment(string segment, string originalInput)
    {
        ValidatePercentEscapes(segment, originalInput);

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(segment);
        }
        catch (UriFormatException exception)
        {
            throw new EpubContainerException(
                EpubContainerErrorCode.InvalidContainerPath,
                $"Escape percentuale non valido nel path OCF: '{originalInput}'.",
                exception);
        }

        if (decoded.Contains('/') || decoded.Contains('\\'))
        {
            throw InvalidPath(
                $"Un segmento percent-encoded non può introdurre separatori di path: '{originalInput}'.");
        }

        return decoded;
    }

    private static void ValidatePercentEscapes(string segment, string originalInput)
    {
        for (int index = 0; index < segment.Length; index++)
        {
            if (segment[index] != '%')
            {
                continue;
            }

            if (index + 2 >= segment.Length ||
                !IsHexDigit(segment[index + 1]) ||
                !IsHexDigit(segment[index + 2]))
            {
                throw InvalidPath($"Escape percentuale non valido nel path OCF: '{originalInput}'.");
            }

            index += 2;
        }
    }

    private static bool IsHexDigit(char value) =>
        value is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';

    private static EpubContainerException InvalidPath(string message) =>
        new(EpubContainerErrorCode.InvalidContainerPath, message);
}
