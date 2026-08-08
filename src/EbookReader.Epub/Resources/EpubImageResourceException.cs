namespace EbookReader.Epub.Resources;

/// <summary>
/// Raised when an EPUB manifest resource cannot be exposed as a bounded local raster image.
/// </summary>
public sealed class EpubImageResourceException : Exception
{
    public EpubImageResourceException(EpubImageResourceErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public EpubImageResourceException(EpubImageResourceErrorCode errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public EpubImageResourceErrorCode ErrorCode { get; }
}
