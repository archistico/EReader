namespace EbookReader.Epub.Navigation;

/// <summary>
/// Exception raised when EPUB navigation cannot be interpreted safely and deterministically.
/// </summary>
public sealed class EpubNavigationException : Exception
{
    public EpubNavigationException()
        : this(EpubNavigationErrorCode.InvalidNavigation, "Navigazione EPUB non valida.")
    {
    }

    public EpubNavigationException(string message)
        : this(EpubNavigationErrorCode.InvalidNavigation, message)
    {
    }

    public EpubNavigationException(string message, Exception innerException)
        : this(EpubNavigationErrorCode.InvalidNavigation, message, innerException)
    {
    }

    public EpubNavigationException(EpubNavigationErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public EpubNavigationException(
        EpubNavigationErrorCode errorCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public EpubNavigationErrorCode ErrorCode { get; }
}
