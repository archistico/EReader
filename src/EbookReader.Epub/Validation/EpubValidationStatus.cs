namespace EbookReader.Epub.Validation;

/// <summary>
/// Overall outcome of EPUB ingestion validation.
/// </summary>
public enum EpubValidationStatus
{
    Valid = 0,
    Invalid = 1,
    Unsupported = 2,
}
