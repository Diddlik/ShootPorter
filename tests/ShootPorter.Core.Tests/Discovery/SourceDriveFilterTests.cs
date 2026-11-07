using ShootPorter.Core.Discovery;

namespace ShootPorter.Core.Tests.Discovery;

/// <summary>
/// Tests for <see cref="SourceDriveFilter"/>.
/// </summary>
public sealed class SourceDriveFilterTests
{
    [Theory]
    [InlineData(DriveType.Removable, false, SourceDriveMode.AutoDetect, true)]
    [InlineData(DriveType.Fixed, true, SourceDriveMode.AutoDetect, true)]
    [InlineData(DriveType.Fixed, false, SourceDriveMode.AutoDetect, false)]
    [InlineData(DriveType.Removable, false, SourceDriveMode.CameraUsb, false)]
    [InlineData(DriveType.Removable, true, SourceDriveMode.CameraUsb, true)]
    [InlineData(DriveType.Fixed, true, SourceDriveMode.CameraUsb, false)]
    [InlineData(DriveType.Removable, false, SourceDriveMode.CardReader, true)]
    [InlineData(DriveType.Fixed, true, SourceDriveMode.CardReader, false)]
    public void WhenDriveIsCheckedThenModeDecidesIfItIsASource(DriveType type, bool hasDcim, SourceDriveMode mode, bool expected)
    {
        Assert.Equal(expected, SourceDriveFilter.IsSource(type, hasDcim, mode));
    }
}
