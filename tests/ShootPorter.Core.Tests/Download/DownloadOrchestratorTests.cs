using ShootPorter.Core.Download;

namespace ShootPorter.Core.Tests.Download;

/// <summary>
/// Tests for <see cref="DownloadOrchestrator"/> batch download coordination.
/// </summary>
public sealed class DownloadOrchestratorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DownloadOrchestrator _orchestrator;

    public DownloadOrchestratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fps_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _orchestrator = new DownloadOrchestrator();
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
    public async Task WhenDownloadingBatchThenAllFilesAreCopied()
    {
        var src1 = CreateTestFile("src/a.jpg");
        var src2 = CreateTestFile("src/b.jpg");
        var destDir = Path.Combine(_tempDir, "dest");

        var filePairs = new[]
        {
            (src1, Path.Combine(destDir, "a.jpg")),
            (src2, Path.Combine(destDir, "b.jpg")),
        };

        var options = new DownloadOptions { MaxParallelism = 2, VerifyAfterCopy = true };

        await _orchestrator.DownloadBatchAsync(filePairs, options);

        Assert.True(File.Exists(Path.Combine(destDir, "a.jpg")));
        Assert.True(File.Exists(Path.Combine(destDir, "b.jpg")));
    }

    [Fact]
    public async Task WhenDownloadingBatchThenReportsCorrectCounts()
    {
        var src1 = CreateTestFile("src/a.jpg");
        var src2 = CreateTestFile("src/b.jpg");
        var missing = Path.Combine(_tempDir, "src", "missing.jpg"); // does not exist
        var destDir = Path.Combine(_tempDir, "dest");

        var filePairs = new[]
        {
            (src1, Path.Combine(destDir, "a.jpg")),
            (src2, Path.Combine(destDir, "b.jpg")),
            (missing, Path.Combine(destDir, "missing.jpg")),
        };

        var options = new DownloadOptions { MaxParallelism = 1, VerifyAfterCopy = false };

        var batchResult = await _orchestrator.DownloadBatchAsync(filePairs, options);

        Assert.Equal(2, batchResult.SucceededCount);
        Assert.Equal(1, batchResult.FailedCount);
        Assert.Equal(3, batchResult.Results.Count);
    }

    [Fact]
    public async Task WhenDownloadingWithBackupThenCopiesToBackup()
    {
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0xAA, 0xBB };
        var source = CreateTestFile("src/photo.jpg", content);
        var destDir = Path.Combine(_tempDir, "dest");
        var backupDir = Path.Combine(_tempDir, "backup");

        var destPath = Path.Combine(destDir, "photo.jpg");
        var filePairs = new[] { (source, destPath) };

        var options = new DownloadOptions
        {
            MaxParallelism = 1,
            VerifyAfterCopy = true,
            BackupDestinations = [backupDir],
        };

        var batchResult = await _orchestrator.DownloadBatchAsync(filePairs, options);

        var backupPath = Path.Combine(backupDir, "photo.jpg");
        Assert.True(File.Exists(destPath), "Primary destination should exist.");
        Assert.True(File.Exists(backupPath), "Backup destination should exist.");
        Assert.Equal(content, File.ReadAllBytes(backupPath));
        Assert.Equal(1, batchResult.SucceededCount);
    }

    [Fact]
    public async Task WhenDownloadingWithBackupAndDestinationRootThenPreservesRelativePath()
    {
        var content = new byte[] { 0x01, 0x02, 0x03 };
        var source = CreateTestFile("src/photo.jpg", content);
        var destRoot = Path.Combine(_tempDir, "dest");
        var backupRoot = Path.Combine(_tempDir, "backup");
        var destPath = Path.Combine(destRoot, "2026", "05", "photo.jpg");
        var filePairs = new[] { (source, destPath) };

        var options = new DownloadOptions
        {
            DestinationRoot = destRoot,
            MaxParallelism = 1,
            VerifyAfterCopy = true,
            BackupDestinations = [backupRoot],
        };

        await _orchestrator.DownloadBatchAsync(filePairs, options);

        var backupPath = Path.Combine(backupRoot, "2026", "05", "photo.jpg");
        Assert.True(File.Exists(backupPath), "Backup should preserve the path relative to the primary destination root.");
        Assert.Equal(content, File.ReadAllBytes(backupPath));
    }
}
