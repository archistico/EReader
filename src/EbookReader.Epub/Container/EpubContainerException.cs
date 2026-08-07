namespace EbookReader.Epub.Container;

/// <summary>
/// Exception raised when an input cannot be treated as a supported EPUB OCF container.
/// </summary>
public sealed class EpubContainerException : Exception
{
    public EpubContainerException()
        : this(EpubContainerErrorCode.InvalidContainer, "Contenitore EPUB non valido.")
    {
    }

    public EpubContainerException(string message)
        : this(EpubContainerErrorCode.InvalidContainer, message)
    {
    }

    public EpubContainerException(string message, Exception innerException)
        : this(EpubContainerErrorCode.InvalidContainer, message, innerException)
    {
    }

    public EpubContainerException(EpubContainerErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public EpubContainerException(
        EpubContainerErrorCode errorCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public EpubContainerErrorCode ErrorCode { get; }
}
