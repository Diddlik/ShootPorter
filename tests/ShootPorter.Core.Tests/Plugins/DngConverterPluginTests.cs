using ShootPorter.Core.Plugins;

namespace ShootPorter.Core.Tests.Plugins;

/// <summary>
/// Tests for <see cref="DngConverterPlugin"/> when Adobe DNG Converter is not installed.
/// </summary>
public sealed class DngConverterPluginTests : IDisposable
{
    private readonly string _tempDir;

    public DngConverterPluginTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fps_dng_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task WhenConverterNotInstalledThenReturnsFalse()
    {
        var cr2Path = Path.Combine(_tempDir, "photo.cr2");
        File.WriteAllBytes(cr2Path, [0x49, 0x49]);
        var plugin = new DngConverterPlugin(dngConverterPath: Path.Combine(_tempDir, "nonexistent_converter.exe"))
        {
            IsEnabled = true,
        };

        var result = await plugin.ProcessAsync(cr2Path);

        Assert.False(result);
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("clip.mp4")]
    [InlineData("document.png")]
    public async Task WhenFileIsNotRawThenReturnsTrue(string fileName)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllBytes(filePath, [0x00]);
        var plugin = new DngConverterPlugin(dngConverterPath: Path.Combine(_tempDir, "nonexistent_converter.exe"))
        {
            IsEnabled = true,
        };

        var result = await plugin.ProcessAsync(filePath);

        Assert.True(result);
    }
}
