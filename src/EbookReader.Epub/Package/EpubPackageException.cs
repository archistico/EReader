namespace EbookReader.Epub.Package;

/// <summary>
/// Exception raised when an OPF Package Document cannot be interpreted safely.
/// </summary>
public sealed class EpubPackageException : Exception
{
    public EpubPackageException()
        : this(EpubPackageErrorCode.InvalidPackage, "Package Document EPUB non valido.")
    {
    }

    public EpubPackageException(string message)
        : this(EpubPackageErrorCode.InvalidPackage, message)
    {
    }

    public EpubPackageException(string message, Exception innerException)
        : this(EpubPackageErrorCode.InvalidPackage, message, innerException)
    {
    }

    public EpubPackageException(EpubPackageErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public EpubPackageException(EpubPackageErrorCode errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public EpubPackageErrorCode ErrorCode { get; }
}
