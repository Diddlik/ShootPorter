namespace ShootPorter.Core.Download;

/// <summary>
/// Describes what happened to the source file after transfer completion.
/// </summary>
public enum SourceFileDisposition
{
    NotRequested,
    Retained,
    Deleted,
    DeleteFailed,
}
