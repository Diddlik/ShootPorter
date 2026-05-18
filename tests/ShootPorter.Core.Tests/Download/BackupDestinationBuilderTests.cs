using ShootPorter.Core.Download;

namespace ShootPorter.Core.Tests.Download;

/// <summary>
/// Tests for <see cref="BackupDestinationBuilder"/> backup root normalization.
/// </summary>
public sealed class BackupDestinationBuilderTests
{
    [Fact]
    public void WhenPathsContainEmptyValuesThenReturnsOnlyUsableBackupRoots()
    {
        var root = Path.Combine(Path.GetTempPath(), "shootporter_backup");

        var result = BackupDestinationBuilder.FromPaths(null, "", "   ", root);

        Assert.Single(result);
        Assert.Equal(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), result[0]);
    }

    [Fact]
    public void WhenPathsContainDuplicatesThenReturnsEachBackupRootOnce()
    {
        var root = Path.Combine(Path.GetTempPath(), "shootporter_backup");
        var sameRootWithSeparator = root + Path.DirectorySeparatorChar;

        var result = BackupDestinationBuilder.FromPaths(root, sameRootWithSeparator);

        Assert.Single(result);
    }
}
