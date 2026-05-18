namespace ShootPorter.Core.Download;

/// <summary>
/// Outcome of a single file download operation.
/// </summary>
public sealed record DownloadResult(
    string SourcePath,
    string DestinationPath,
    bool Success,
    long BytesCopied,
    string? ErrorMessage = null);
