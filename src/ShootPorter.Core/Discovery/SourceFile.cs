namespace ShootPorter.Core.Discovery;

/// <summary>
/// Represents a discovered photo or video file on a source device or folder.
/// </summary>
public sealed record SourceFile(
    string FullPath,
    string FileName,
    string Extension,
    long SizeBytes,
    DateTimeOffset LastModified,
    FileCategory Category);
