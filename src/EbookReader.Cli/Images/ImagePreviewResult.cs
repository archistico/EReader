namespace EbookReader.Cli.Images;

internal sealed record ImagePreviewResult(bool Succeeded, string Message)
{
    public static ImagePreviewResult Success() => new(true, "Immagine aperta con il viewer di sistema.");

    public static ImagePreviewResult Failure(string message) => new(false, message);
}
