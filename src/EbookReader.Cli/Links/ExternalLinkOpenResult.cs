namespace EbookReader.Cli.Links;

internal sealed record ExternalLinkOpenResult(bool Succeeded, string Message)
{
    public static ExternalLinkOpenResult Success() => new(true, "Link aperto nel browser/applicazione di sistema.");

    public static ExternalLinkOpenResult Failure(string message) => new(false, message);
}
