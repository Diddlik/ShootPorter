using ShootPorter.Core.Dedup;

namespace ShootPorter.Core.Tests.Dedup;

/// <summary>
/// Tests for <see cref="FileComparer"/> source-to-destination comparison logic.
/// </summary>
public sealed class FileComparerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileComparer _comparer;

    public FileComparerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fps_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _comparer = new FileComparer();
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
    public void WhenDestinationDoesNotExistThenStatusIsNew()
    {
        var source = CreateTestFile("source.jpg");
        var dest = Path.Combine(_tempDir, "dest", "source.jpg");

        var result = _comparer.Compare(source, dest);

        Assert.Equal(FileStatus.New, result.Status);
        Assert.Null(result.DestinationSizeBytes);
    }

    [Fact]
    public void WhenFilesHaveSameSizeAndTimeThenStatusIsDownloaded()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var source = CreateTestFile("source.jpg", content);
        var dest = CreateTestFile("dest.jpg", content);

        // Synchronise timestamps (within 2-second window)
        var ts = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(source, ts);
        File.SetLastWriteTimeUtc(dest, ts);

        var result = _comparer.Compare(source, dest);

        Assert.Equal(FileStatus.Downloaded, result.Status);
        Assert.Equal(result.SourceSizeBytes, result.DestinationSizeBytes);
    }

    [Fact]
    public void WhenFilesHaveDifferentSizesThenStatusIsSizeMismatch()
    {
        var source = CreateTestFile("source.jpg", [0xFF, 0xD8, 0xFF, 0xE0]);
        var dest = CreateTestFile("dest.jpg", [0xFF, 0xD8]);

        var result = _comparer.Compare(source, dest);

        Assert.Equal(FileStatus.SizeMismatch, result.Status);
        Assert.NotEqual(result.SourceSizeBytes, result.DestinationSizeBytes);
    }

    [Fact]
    public void WhenFilesHaveSameSizeButDifferentTimeThenStatusIsTimeMismatch()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var source = CreateTestFile("source.jpg", content);
        var dest = CreateTestFile("dest.jpg", content);

        File.SetLastWriteTimeUtc(source, new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(dest, new DateTime(2024, 6, 15, 10, 0, 5, DateTimeKind.Utc)); // 5-second gap

        var result = _comparer.Compare(source, dest);

        Assert.Equal(FileStatus.TimeMismatch, result.Status);
    }

    [Fact]
    public async Task WhenFilesAreIdenticalThenAreFilesIdenticalReturnsTrue()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        var path1 = CreateTestFile("a.jpg", content);
        var path2 = CreateTestFile("b.jpg", content);

        var result = await _comparer.AreFilesIdenticalAsync(path1, path2);

        Assert.True(result);
    }

    [Fact]
    public async Task WhenFilesHaveDifferentContentThenAreFilesIdenticalReturnsFalse()
    {
        var path1 = CreateTestFile("a.jpg", [0xFF, 0xD8, 0xFF, 0xE0]);
        var path2 = CreateTestFile("b.jpg", [0xFF, 0xD8, 0xFF, 0xDB]);

        var result = await _comparer.AreFilesIdenticalAsync(path1, path2);

        Assert.False(result);
    }
}
