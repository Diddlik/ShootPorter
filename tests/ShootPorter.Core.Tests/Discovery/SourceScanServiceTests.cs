using ShootPorter.Core.Discovery;
using ShootPorter.Core.Metadata;

namespace ShootPorter.Core.Tests.Discovery;

/// <summary>
/// Tests for <see cref="SourceScanService"/> chunking and metadata handling.
/// </summary>
public sealed class SourceScanServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"sp_scan_{Guid.NewGuid():N}");

    public SourceScanServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private sealed class StubMetadataReader(Func<string, FileMetadata> read) : IMetadataReader
    {
        public Task<FileMetadata> ReadMetadataAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(read(filePath));
    }

    private void CreateFiles(int count)
    {
        for (var i = 0; i < count; i++)
            File.WriteAllBytes(Path.Combine(_tempDir, $"IMG_{i:D3}.jpg"), [0xFF, 0xD8]);
    }

    private static async Task<List<IReadOnlyList<ScannedFile>>> ScanAllAsync(SourceScanService service, string path, int chunkSize)
    {
        var chunks = new List<IReadOnlyList<ScannedFile>>();
        await foreach (var chunk in service.ScanAsync(path, chunkSize))
            chunks.Add(chunk);
        return chunks;
    }

    [Fact]
    public async Task WhenFolderHasMoreFilesThanChunkSizeThenFilesAreSplitIntoChunks()
    {
        CreateFiles(5);
        var service = new SourceScanService(new FileSystemScanner(), new StubMetadataReader(p => new FileMetadata { FilePath = p }));

        var chunks = await ScanAllAsync(service, _tempDir, chunkSize: 2);

        Assert.Equal([2, 2, 1], chunks.Select(c => c.Count));
    }

    [Fact]
    public async Task WhenMetadataHasCaptureDateThenItIsUsedAsCaptureDate()
    {
        CreateFiles(1);
        var captured = new DateTimeOffset(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);
        var service = new SourceScanService(new FileSystemScanner(),
            new StubMetadataReader(p => new FileMetadata { FilePath = p, CaptureDateTime = captured }));

        var file = (await ScanAllAsync(service, _tempDir, 64)).Single().Single();

        Assert.Equal(captured.DateTime, file.CaptureDate);
    }

    [Fact]
    public async Task WhenMetadataCannotBeReadThenFileIsKeptWithoutMetadata()
    {
        CreateFiles(1);
        var service = new SourceScanService(new FileSystemScanner(),
            new StubMetadataReader(_ => throw new InvalidDataException("corrupt")));

        var file = (await ScanAllAsync(service, _tempDir, 64)).Single().Single();

        Assert.Null(file.Metadata);
    }
}
