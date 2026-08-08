namespace EbookReader.Application.Diagnostics;

/// <summary>
/// User-relevant outcome of an EReader operation.
/// </summary>
public enum ReaderOperationStatus
{
    Success = 0,
    SuccessWithDiagnostics = 1,
    DocumentUnreadable = 2,
    InternalFailure = 3,
}
