using ShootPorter.Core.Download;

namespace ShootPorter.Core.Tests.Download;

/// <summary>
/// Tests for <see cref="FileCopier"/> single-file copy operations.
/// </summary>
public sealed class FileCopierTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileCopier _copier;

    public FileCopierTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fps_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _copier = new FileCopier();
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
    public async Task WhenCopyingFileThenDestinationMatchesSource()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02 };
        var source = CreateTestFile("src/photo.jpg", content);
        var dest = Path.Combine(_tempDir, "dest", "photo.jpg");

        var result = await _copier.CopyFileAsync(source, dest, 81920, verifyAfterCopy: true, null, 1, 1);

        Assert.True(result.Success);
        Assert.True(File.Exists(dest));
        Assert.Equal(content, File.ReadAllBytes(dest));
    }

    [Fact]
    public async Task WhenSourceDoesNotExistThenReturnsFailed()
    {
        var source = Path.Combine(_tempDir, "nonexistent.jpg");
        var dest = Path.Combine(_tempDir, "dest", "nonexistent.jpg");

        var result = await _copier.CopyFileAsync(source, dest, 81920, verifyAfterCopy: false, null, 1, 1);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task WhenCopyingThenPreservesLastWriteTime()
    {
        var source = CreateTestFile("src/photo.jpg");
        var sourceTime = new DateTime(2023, 5, 20, 8, 30, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(source, sourceTime);

        var dest = Path.Combine(_tempDir, "dest", "photo.jpg");

        await _copier.CopyFileAsync(source, dest, 81920, verifyAfterCopy: false, null, 1, 1);

        var destTime = File.GetLastWriteTimeUtc(dest);
        Assert.Equal(sourceTime, destTime);
    }

    [Fact]
    public async Task WhenCopyingThenReportsProgress()
    {
        var content = new byte[256 * 1024]; // 256 KB — forces multiple buffer reads
        new Random(42).NextBytes(content);
        var source = CreateTestFile("src/large.jpg", content);
        var dest = Path.Combine(_tempDir, "dest", "large.jpg");

        var reports = new List<DownloadProgress>();
        var progress = new Progress<DownloadProgress>(p => reports.Add(p));

        await _copier.CopyFileAsync(source, dest, 65536, verifyAfterCopy: false, progress, 1, 3);

        // Allow progress callbacks to flush (Progress<T> posts to synchronization context)
        await Task.Delay(50);

        Assert.NotEmpty(reports);
        Assert.All(reports, p =>
        {
            Assert.Equal(source, p.SourcePath);
            Assert.Equal(dest, p.DestinationPath);
            Assert.Equal(3, p.TotalFiles);
        });
        // Final report should show all bytes copied
        var last = reports[^1];
        Assert.Equal(last.TotalBytes, last.BytesCopied);
    }
}
