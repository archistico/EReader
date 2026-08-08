namespace EbookReader.Application.Diagnostics;

/// <summary>
/// High-level deterministic action taken after a diagnosed condition.
/// </summary>
public enum ReaderRecoveryAction
{
    None = 0,
    Continue = 1,
    ContinueDegraded = 2,
    RejectDocument = 3,
    KeepCurrentLocation = 4,
    UseFallback = 5,
}
