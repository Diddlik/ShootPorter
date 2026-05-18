namespace ShootPorter.Core.Dedup;

/// <summary>
/// Result of comparing a source file against a destination path.
/// </summary>
public sealed record FileComparisonResult(
    string SourcePath,
    string DestinationPath,
    FileStatus Status,
    long SourceSizeBytes,
    long? DestinationSizeBytes);
