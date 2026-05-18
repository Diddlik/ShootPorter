namespace ShootPorter.Core.Download;

/// <summary>
/// Reports the progress of a file download operation.
/// </summary>
public sealed record DownloadProgress(
    string SourcePath,
    string DestinationPath,
    long BytesCopied,
    long TotalBytes,
    int FileIndex,
    int TotalFiles)
{
    /// <summary>Progress as a value between 0.0 and 1.0 for the current file.</summary>
    public double FileProgressFraction => TotalBytes > 0 ? (double)BytesCopied / TotalBytes : 1.0;

    /// <summary>Progress as a value between 0.0 and 1.0 for the entire batch.</summary>
    public double BatchProgressFraction => TotalFiles > 0 ? (double)(FileIndex - 1 + FileProgressFraction) / TotalFiles : 1.0;
}
