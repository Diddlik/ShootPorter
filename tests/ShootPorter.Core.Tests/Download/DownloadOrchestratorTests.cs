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

    [Fact]
    public async Task WhenAutoDeleteEnabledAndVerifiedTransferSucceedsThenDeletesSource()
    {
        var source = CreateTestFile("src/photo.jpg");
        var destPath = Path.Combine(_tempDir, "dest", "photo.jpg");
        var filePairs = new[] { (source, destPath) };

        var options = new DownloadOptions
        {
            MaxParallelism = 1,
            VerifyAfterCopy = true,
            AutoDeleteSource = true,
        };

        var batchResult = await _orchestrator.DownloadBatchAsync(filePairs, options);
        var result = Assert.Single(batchResult.Results);

        Assert.True(result.Success);
        Assert.True(File.Exists(destPath), "Destination file should remain after source deletion.");
        Assert.False(File.Exists(source), "Source file should be deleted after verified transfer.");
        Assert.Equal(SourceFileDisposition.Deleted, result.SourceDisposition);
    }

    [Fact]
    public async Task WhenAutoDeleteEnabledButVerificationDisabledThenRetainsSource()
    {
        var source = CreateTestFile("src/photo.jpg");
        var destPath = Path.Combine(_tempDir, "dest", "photo.jpg");
        var filePairs = new[] { (source, destPath) };

        var options = new DownloadOptions
        {
            MaxParallelism = 1,
            VerifyAfterCopy = false,
            AutoDeleteSource = true,
        };

        var batchResult = await _orchestrator.DownloadBatchAsync(filePairs, options);
        var result = Assert.Single(batchResult.Results);

        Assert.True(result.Success);
        Assert.True(File.Exists(destPath));
        Assert.True(File.Exists(source), "Source file should be retained when verification is disabled.");
        Assert.Equal(SourceFileDisposition.Retained, result.SourceDisposition);
    }

    [Fact]
    public async Task WhenBackupFailsThenAutoDeleteRetainsSourceAndReportsFailure()
    {
        var source = CreateTestFile("src/photo.jpg");
        var destPath = Path.Combine(_tempDir, "dest", "photo.jpg");
        var invalidBackupRoot = Path.Combine(_tempDir, "backup-as-file");
        File.WriteAllText(invalidBackupRoot, "not a directory");
        var filePairs = new[] { (source, destPath) };

        var options = new DownloadOptions
        {
            MaxParallelism = 1,
            VerifyAfterCopy = true,
            AutoDeleteSource = true,
            BackupDestinations = [invalidBackupRoot],
        };

        var batchResult = await _orchestrator.DownloadBatchAsync(filePairs, options);
        var result = Assert.Single(batchResult.Results);

        Assert.False(result.Success);
        Assert.True(File.Exists(destPath), "Primary destination should remain when backup fails.");
        Assert.True(File.Exists(source), "Source file should be retained when backup copy fails.");
        Assert.Equal(SourceFileDisposition.Retained, result.SourceDisposition);
        Assert.Contains("Backup failed", result.ErrorMessage);
    }

    [Fact]
    public async Task WhenSourceDeleteFailsThenKeepsDestinationAndReportsDeleteFailure()
    {
        var source = CreateTestFile("src/photo.jpg");
        var destPath = Path.Combine(_tempDir, "dest", "photo.jpg");
        var filePairs = new[] { (source, destPath) };

        await using var sourceLock = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);

        var options = new DownloadOptions
        {
            MaxParallelism = 1,
            VerifyAfterCopy = true,
            AutoDeleteSource = true,
        };

        var batchResult = await _orchestrator.DownloadBatchAsync(filePairs, options);
        var result = Assert.Single(batchResult.Results);

        Assert.False(result.Success);
        Assert.True(File.Exists(destPath), "Destination file should remain when source delete fails.");
        Assert.True(File.Exists(source), "Source file should remain when delete fails.");
        Assert.Equal(SourceFileDisposition.DeleteFailed, result.SourceDisposition);
        Assert.Contains("Source delete failed", result.ErrorMessage);
    }

    [Fact]
    public async Task WhenResolverChoosesSuffixThenKeepsExistingFileAndWritesUniqueName()
    {
        var source = CreateTestFile("src/photo.jpg", [0x01, 0x02, 0x03]);
        var existing = CreateTestFile("dest/photo.jpg", [0x09]);
        string? askedPath = null;
        var options = new DownloadOptions
        {
            MaxParallelism = 1,
            ResolveExistingFile = (path, _) =>
            {
                askedPath = path;
                return Task.FromResult(ConflictResolution.AddSuffix);
            },
        };

        var batchResult = await _orchestrator.DownloadBatchAsync([(source, existing)], options);
        var result = Assert.Single(batchResult.Results);

        Assert.Equal(existing, askedPath);
        Assert.True(result.Success);
        Assert.Equal(new byte[] { 0x09 }, File.ReadAllBytes(existing));
        Assert.Equal(Path.Combine(_tempDir, "dest", "photo_01.jpg"), result.DestinationPath);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, File.ReadAllBytes(result.DestinationPath));
    }

    [Fact]
    public async Task WhenResolverChoosesOverwriteThenReplacesExistingFile()
    {
        var source = CreateTestFile("src/photo.jpg", [0x01, 0x02, 0x03]);
        var existing = CreateTestFile("dest/photo.jpg", [0x09]);
        var options = new DownloadOptions
        {
            MaxParallelism = 1,
            ResolveExistingFile = (_, _) => Task.FromResult(ConflictResolution.Overwrite),
        };

        var batchResult = await _orchestrator.DownloadBatchAsync([(source, existing)], options);
        var result = Assert.Single(batchResult.Results);

        Assert.True(result.Success);
        Assert.Equal(existing, result.DestinationPath);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, File.ReadAllBytes(existing));
        Assert.Single(Directory.GetFiles(Path.Combine(_tempDir, "dest")));
    }

    [Fact]
    public async Task WhenNoResolverIsSetThenExistingFileIsKept()
    {
        var source = CreateTestFile("src/photo.jpg", [0x01]);
        var existing = CreateTestFile("dest/photo.jpg", [0x09]);

        var batchResult = await _orchestrator.DownloadBatchAsync(
            [(source, existing)], new DownloadOptions { MaxParallelism = 1 });

        Assert.Equal(new byte[] { 0x09 }, File.ReadAllBytes(existing));
        Assert.NotEqual(existing, Assert.Single(batchResult.Results).DestinationPath);
    }

    [Fact]
    public async Task WhenTwoSourcesShareADestinationThenBothAreKept()
    {
        var src1 = CreateTestFile("a/photo.jpg", [0x01]);
        var src2 = CreateTestFile("b/photo.jpg", [0x02, 0x03]);
        var destPath = Path.Combine(_tempDir, "dest", "photo.jpg");
        var options = new DownloadOptions { MaxParallelism = 2 };

        var batchResult = await _orchestrator.DownloadBatchAsync([(src1, destPath), (src2, destPath)], options);

        Assert.Equal(2, batchResult.SucceededCount);
        Assert.Equal(2, batchResult.Results.Select(r => r.DestinationPath).Distinct().Count());
    }

    [Fact]
    public async Task WhenFileFinishesThenItsResultIsReportedImmediately()
    {
        var src1 = CreateTestFile("src/a.jpg");
        var src2 = CreateTestFile("src/b.jpg");
        var destDir = Path.Combine(_tempDir, "dest");
        var reported = new System.Collections.Concurrent.ConcurrentBag<DownloadResult>();
        var fileCompleted = new SyncProgress<DownloadResult>(reported.Add);

        await _orchestrator.DownloadBatchAsync(
            [(src1, Path.Combine(destDir, "a.jpg")), (src2, Path.Combine(destDir, "b.jpg"))],
            new DownloadOptions { MaxParallelism = 2 },
            fileCompleted: fileCompleted);

        Assert.Equal(2, reported.Count);
        Assert.All(reported, r => Assert.True(r.Success));
    }

    [Fact]
    public async Task WhenSourceHasSidecarsThenTheyAreCopiedUnderTheTargetName()
    {
        var source = CreateTestFile("src/IMG_0001.CR3");
        CreateTestFile("src/IMG_0001.xmp");
        CreateTestFile("src/IMG_0001.CR3.thm");
        var destDir = Path.Combine(_tempDir, "dest");

        await _orchestrator.DownloadBatchAsync([(source, Path.Combine(destDir, "2026-11-07_001.CR3"))], new DownloadOptions());

        Assert.True(File.Exists(Path.Combine(destDir, "2026-11-07_001.xmp")));
        Assert.True(File.Exists(Path.Combine(destDir, "2026-11-07_001.CR3.thm")));
    }

    [Fact]
    public async Task WhenTwoFilesShareASidecarThenItIsCopiedOnceWithoutAConflictQuestion()
    {
        var raw = CreateTestFile("src/IMG_0001.CR3");
        var jpg = CreateTestFile("src/IMG_0001.JPG");
        CreateTestFile("src/IMG_0001.xmp");
        var destDir = Path.Combine(_tempDir, "dest");
        var asked = false;
        var options = new DownloadOptions
        {
            ResolveExistingFile = (_, _) =>
            {
                asked = true;
                return Task.FromResult(ConflictResolution.AddSuffix);
            },
        };

        var batch = await _orchestrator.DownloadBatchAsync(
            [(raw, Path.Combine(destDir, "IMG_0001.CR3")), (jpg, Path.Combine(destDir, "IMG_0001.JPG"))], options);

        Assert.All(batch.Results, r => Assert.True(r.Success));
        Assert.False(asked);
        Assert.Single(Directory.GetFiles(destDir, "*.xmp"));
    }

    [Fact]
    public async Task WhenOnlySidecarTargetExistsThenUserIsAskedAndExistingSidecarIsKept()
    {
        var source = CreateTestFile("src/IMG_0001.CR3");
        CreateTestFile("src/IMG_0001.xmp");
        var existingSidecar = CreateTestFile("dest/IMG_0001.xmp");
        File.WriteAllText(existingSidecar, "keep");
        string? askedFor = null;
        var options = new DownloadOptions
        {
            ResolveExistingFile = (path, _) =>
            {
                askedFor = path;
                return Task.FromResult(ConflictResolution.AddSuffix);
            },
        };

        var batch = await _orchestrator.DownloadBatchAsync([(source, Path.Combine(_tempDir, "dest", "IMG_0001.CR3"))], options);

        Assert.True(batch.Results.Single().Success);
        Assert.Equal(Path.GetFullPath(existingSidecar), askedFor);
        Assert.Equal("keep", File.ReadAllText(existingSidecar));
        Assert.Equal(2, Directory.GetFiles(Path.Combine(_tempDir, "dest"), "*.xmp").Length);
    }

    [Fact]
    public async Task WhenMainFileIsOverwrittenThenItsSidecarIsOverwrittenWithoutAsking()
    {
        var source = CreateTestFile("src/IMG_0001.CR3");
        CreateTestFile("src/IMG_0001.xmp");
        CreateTestFile("dest/IMG_0001.CR3");
        var existingSidecar = CreateTestFile("dest/IMG_0001.xmp");
        File.WriteAllText(existingSidecar, "old");
        var asked = 0;
        var options = new DownloadOptions
        {
            ResolveExistingFile = (_, _) =>
            {
                asked++;
                return Task.FromResult(ConflictResolution.Overwrite);
            },
        };

        await _orchestrator.DownloadBatchAsync([(source, Path.Combine(_tempDir, "dest", "IMG_0001.CR3"))], options);

        Assert.Equal(1, asked);
        Assert.NotEqual("old", File.ReadAllText(existingSidecar));
    }

    [Fact]
    public async Task WhenAutoDeleteSucceedsThenSidecarSourceIsDeletedToo()
    {
        var source = CreateTestFile("src/IMG_0001.CR3");
        var sidecar = CreateTestFile("src/IMG_0001.xmp");
        var options = new DownloadOptions { VerifyAfterCopy = true, AutoDeleteSource = true };

        await _orchestrator.DownloadBatchAsync([(source, Path.Combine(_tempDir, "dest", "IMG_0001.CR3"))], options);

        Assert.False(File.Exists(sidecar));
        Assert.True(File.Exists(Path.Combine(_tempDir, "dest", "IMG_0001.xmp")));
    }

    private sealed class SyncProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
