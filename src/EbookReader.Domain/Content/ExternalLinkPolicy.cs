namespace EbookReader.Domain.Content;

/// <summary>
/// Format-neutral allow-list for external hyperlinks that EReader may expose as actionable.
/// The policy does not open, fetch or otherwise resolve the URI.
/// </summary>
public static class ExternalLinkPolicy
{
    public static bool IsAllowed(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return uri.IsAbsoluteUri &&
               (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.Scheme, "mailto", StringComparison.OrdinalIgnoreCase));
    }
}
