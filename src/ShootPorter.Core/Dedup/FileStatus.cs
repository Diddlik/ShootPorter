namespace ShootPorter.Core.Dedup;

/// <summary>
/// Indicates the deduplication status of a source file relative to a destination.
/// </summary>
public enum FileStatus
{
    /// <summary>File does not exist at destination — safe to download.</summary>
    New,
    /// <summary>File exists at destination with matching size and timestamp — already downloaded.</summary>
    Downloaded,
    /// <summary>File exists at destination with identical content (hash match).</summary>
    Duplicate,
    /// <summary>File exists at destination but size differs — possible corruption or modification.</summary>
    SizeMismatch,
    /// <summary>File exists at destination but last-modified time differs.</summary>
    TimeMismatch,
}
