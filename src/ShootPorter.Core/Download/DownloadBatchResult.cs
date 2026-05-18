namespace ShootPorter.Core.Download;

/// <summary>
/// Summary of an entire download batch operation.
/// </summary>
public sealed record DownloadBatchResult(
    IReadOnlyList<DownloadResult> Results,
    int SucceededCount,
    int FailedCount,
    long TotalBytesCopied,
    TimeSpan Duration);
