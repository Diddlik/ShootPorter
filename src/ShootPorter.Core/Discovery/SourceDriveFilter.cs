namespace ShootPorter.Core.Discovery;

/// <summary>
/// Which drives are offered as download sources.
/// </summary>
public enum SourceDriveMode
{
    /// <summary>Removable drives, plus fixed drives with a DCIM folder.</summary>
    AutoDetect,

    /// <summary>Removable drives with a DCIM folder (camera convention).</summary>
    CameraUsb,

    /// <summary>All removable drives.</summary>
    CardReader,
}

/// <summary>
/// Decides whether a drive is a download source for a <see cref="SourceDriveMode"/>.
/// </summary>
public static class SourceDriveFilter
{
    public static bool IsSource(DriveType driveType, bool hasDcim, SourceDriveMode mode) => mode switch
    {
        SourceDriveMode.CameraUsb => driveType == DriveType.Removable && hasDcim,
        SourceDriveMode.CardReader => driveType == DriveType.Removable,
        _ => driveType == DriveType.Removable || (driveType == DriveType.Fixed && hasDcim),
    };
}
