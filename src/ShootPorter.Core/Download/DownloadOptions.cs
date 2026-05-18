namespace ShootPorter.Core.Download;

/// <summary>
/// Configuration for a download batch operation.
/// </summary>
public sealed record DownloadOptions
{
    /// <summary>Maximum parallel file copies. Defaults to 2.</summary>
    public int MaxParallelism { get; init; } = 2;

    /// <summary>Buffer size for file copy streams in bytes. Defaults to 81920 (80 KB).</summary>
    public int BufferSize { get; init; } = 81920;

    /// <summary>Whether to verify file integrity after copy by comparing sizes.</summary>
    public bool VerifyAfterCopy { get; init; } = true;

    /// <summary>Primary destination root. Used to preserve relative folders in backup destinations.</summary>
    public string? DestinationRoot { get; init; }

    /// <summary>Optional backup destination paths. Files are copied here in addition to the primary destination.</summary>
    public IReadOnlyList<string> BackupDestinations { get; init; } = [];
}
