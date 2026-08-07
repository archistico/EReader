namespace EbookReader.Epub.Content;

/// <summary>
/// Exception raised while projecting EPUB Content Documents into the format-neutral Domain model.
/// </summary>
public sealed class EpubContentException : Exception
{
    public EpubContentException(EpubContentErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public EpubContentException(EpubContentErrorCode errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public EpubContentErrorCode ErrorCode { get; }
}
