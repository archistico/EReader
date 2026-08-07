namespace EbookReader.Domain.Content;

public sealed class ExternalLinkTarget : LinkTarget
{
    public ExternalLinkTarget(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("Un link esterno deve usare un URI assoluto.", nameof(uri));
        }

        Uri = uri;
    }

    public Uri Uri { get; }
}
