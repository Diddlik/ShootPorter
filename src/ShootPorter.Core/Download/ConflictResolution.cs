namespace ShootPorter.Core.Download;

/// <summary>
/// What to do when a download target already exists on disk.
/// </summary>
public enum ConflictResolution
{
    Overwrite,
    AddSuffix,
}
