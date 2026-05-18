using ShootPorter.Core.Plugins;

namespace ShootPorter.Core.Tests.Plugins;

/// <summary>
/// Tests for <see cref="DirectoryMakerPlugin"/> subdirectory creation.
/// </summary>
public sealed class DirectoryMakerPluginTests : IDisposable
{
    private readonly string _tempDir;

    public DirectoryMakerPluginTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fps_dirmaker_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task WhenProcessingFileThenCreatesSubdirectories()
    {
        var filePath = Path.Combine(_tempDir, "photo.jpg");
        File.WriteAllBytes(filePath, [0xFF, 0xD8, 0xFF]);
        var plugin = new DirectoryMakerPlugin();

        var result = await plugin.ProcessAsync(filePath);

        Assert.True(result);
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "originals")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "edited")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "exports")));
    }

    [Fact]
    public async Task WhenCustomSubdirectoriesThenCreatesThose()
    {
        var filePath = Path.Combine(_tempDir, "photo.cr2");
        File.WriteAllBytes(filePath, [0x00]);
        var plugin = new DirectoryMakerPlugin(["raw", "processed"]);

        var result = await plugin.ProcessAsync(filePath);

        Assert.True(result);
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "raw")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "processed")));
        Assert.False(Directory.Exists(Path.Combine(_tempDir, "originals")));
    }
}
