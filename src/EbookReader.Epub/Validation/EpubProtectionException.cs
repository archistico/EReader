namespace EbookReader.Epub.Validation;

/// <summary>
/// Exception raised when protection metadata is malformed or violates the OCF contract.
/// </summary>
public sealed class EpubProtectionException : Exception
{
    public EpubProtectionException(EpubProtectionErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public EpubProtectionException(
        EpubProtectionErrorCode errorCode,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public EpubProtectionErrorCode ErrorCode { get; }
}
