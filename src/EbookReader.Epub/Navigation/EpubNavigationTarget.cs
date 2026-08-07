using EbookReader.Epub.Container;

namespace EbookReader.Epub.Navigation;

/// <summary>
/// Resolved destination used by the supported EPUB navigation aids.
/// These aids are constrained to local publication content.
/// </summary>
public sealed class EpubNavigationTarget
{
    internal EpubNavigationTarget(string href, OcfPath localPath, string? fragment)
    {
        Href = href;
        LocalPath = localPath;
        Fragment = fragment;
    }

    public string Href { get; }

    public OcfPath LocalPath { get; }

    public string? Fragment { get; }
}
