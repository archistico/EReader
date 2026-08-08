namespace EbookReader.Cli.Links;

internal interface IReaderExternalLinkService
{
    ExternalLinkOpenResult Open(Uri uri);
}
