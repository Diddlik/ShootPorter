using ShootPorter.Core.Dedup;

namespace ShootPorter.Core.Tests.Dedup;

/// <summary>
/// Tests for <see cref="DuplicateChecker"/> batch file comparison logic.
/// </summary>
public sealed class DuplicateCheckerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DuplicateChecker _checker;

    public DuplicateCheckerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fps_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _checker = new DuplicateChecker();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string CreateTestFile(string relativePath, byte[]? content = null)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, content ?? [0xFF, 0xD8, 0xFF, 0xE0]);
        return fullPath;
    }

    [Fact]
    public void WhenAllFilesAreNewThenAllStatusesAreNew()
    {
        var source1 = CreateTestFile("src/a.jpg");
        var source2 = CreateTestFile("src/b.jpg");

        var destDir = Path.Combine(_tempDir, "dest");
        var sourcePaths = new[] { source1, source2 };

        var results = _checker.CheckFiles(sourcePaths, src => Path.Combine(destDir, Path.GetFileName(src)));

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(FileStatus.New, r.Status));
    }

    [Fact]
    public void WhenSomeFilesExistThenMixedStatuses()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var source1 = CreateTestFile("src/a.jpg", content);
        var source2 = CreateTestFile("src/b.jpg", content);

        // Create destination for only the first file with matching size and time
        var destPath1 = CreateTestFile("dest/a.jpg", content);
        var ts = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(source1, ts);
        File.SetLastWriteTimeUtc(destPath1, ts);

        var destDir = Path.Combine(_tempDir, "dest");
        var sourcePaths = new[] { source1, source2 };

        var results = _checker.CheckFiles(sourcePaths, src => Path.Combine(destDir, Path.GetFileName(src)));

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Status == FileStatus.Downloaded);
        Assert.Contains(results, r => r.Status == FileStatus.New);
    }

    [Fact]
    public void WhenGetNewFilesThenOnlyReturnsNewStatus()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var source1 = CreateTestFile("src/a.jpg", content);
        var source2 = CreateTestFile("src/b.jpg", content);
        var source3 = CreateTestFile("src/c.jpg", content);

        // source1 already has a matching destination
        var destPath1 = CreateTestFile("dest/a.jpg", content);
        var ts = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(source1, ts);
        File.SetLastWriteTimeUtc(destPath1, ts);

        var destDir = Path.Combine(_tempDir, "dest");
        var sourcePaths = new[] { source1, source2, source3 };

        var newFiles = _checker.GetNewFiles(sourcePaths, src => Path.Combine(destDir, Path.GetFileName(src)));

        Assert.Equal(2, newFiles.Count);
        Assert.All(newFiles, r => Assert.Equal(FileStatus.New, r.Status));
        Assert.DoesNotContain(newFiles, r => Path.GetFileName(r.SourcePath) == "a.jpg");
    }
}
