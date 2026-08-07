using EbookReader.Epub.Container;

namespace EbookReader.Epub.Validation;

/// <summary>
/// One resource declared by META-INF/encryption.xml.
/// </summary>
public sealed record EpubProtectedResource
{
    public EpubProtectedResource(OcfPath path, string? algorithm, EpubProtectionKind kind)
    {
        ArgumentNullException.ThrowIfNull(path);

        Path = path;
        Algorithm = string.IsNullOrWhiteSpace(algorithm) ? null : algorithm;
        Kind = kind;
    }

    public OcfPath Path { get; }

    public string? Algorithm { get; }

    public EpubProtectionKind Kind { get; }
}
