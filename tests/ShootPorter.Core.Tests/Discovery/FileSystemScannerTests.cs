using ShootPorter.Core.Discovery;

namespace ShootPorter.Core.Tests.Discovery;

/// <summary>
/// Tests for <see cref="FileSystemScanner"/> file enumeration.
/// </summary>
public sealed class FileSystemScannerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSystemScanner _scanner;

    public FileSystemScannerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fps_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _scanner = new FileSystemScanner();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string CreateTestFile(string relativePath)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(fullPath, [0xFF, 0xD8, 0xFF]);
        return fullPath;
    }

    [Fact]
    public async Task WhenScanningDirectoryWithImagesThenReturnsSourceFiles()
    {
        CreateTestFile("photo.jpg");

        var files = new List<SourceFile>();
        await foreach (var f in _scanner.ScanDirectoryAsync(_tempDir, recursive: false))
            files.Add(f);

        Assert.Single(files);
        Assert.Equal(".jpg", files[0].Extension);
        Assert.Equal(FileCategory.Image, files[0].Category);
        Assert.Equal(3, files[0].SizeBytes);
    }

    [Fact]
    public async Task WhenScanningDirectoryWithMixedFilesThenOnlyReturnsSupported()
    {
        CreateTestFile("photo.jpg");
        CreateTestFile("readme.txt");
        CreateTestFile("video.mp4");

        var files = new List<SourceFile>();
        await foreach (var f in _scanner.ScanDirectoryAsync(_tempDir, recursive: false))
            files.Add(f);

        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.Extension == ".jpg");
        Assert.Contains(files, f => f.Extension == ".mp4");
    }

    [Fact]
    public async Task WhenScanningEmptyDirectoryThenReturnsNoFiles()
    {
        var files = new List<SourceFile>();
        await foreach (var f in _scanner.ScanDirectoryAsync(_tempDir, recursive: false))
            files.Add(f);

        Assert.Empty(files);
    }

    [Fact]
    public async Task WhenScanningNonExistentDirectoryThenThrowsDirectoryNotFound()
    {
        var bogus = Path.Combine(_tempDir, "does_not_exist");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
        {
            await foreach (var _ in _scanner.ScanDirectoryAsync(bogus, recursive: false))
            { }
        });
    }

    [Fact]
    public async Task WhenScanningWithNullPathThenThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in _scanner.ScanDirectoryAsync(null!, recursive: false))
            { }
        });
    }

    [Fact]
    public async Task WhenScanningRecursivelyThenFindsFilesInSubdirs()
    {
        CreateTestFile("sub/deep/raw.cr2");
        CreateTestFile("top.nef");

        var files = new List<SourceFile>();
        await foreach (var f in _scanner.ScanDirectoryAsync(_tempDir, recursive: true))
            files.Add(f);

        Assert.Equal(2, files.Count);
    }
}
