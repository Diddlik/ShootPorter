using ShootPorter.Core.Profiles;

namespace ShootPorter.Core.Tests.Profiles;

/// <summary>
/// Tests for <see cref="AppSettingsStore"/> persistence and corruption handling.
/// </summary>
public sealed class AppSettingsStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"sp_settings_{Guid.NewGuid():N}");
    private string FilePath => Path.Combine(_tempDir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task WhenSavedThenLoadReturnsSameSettingsAndNoTempFileRemains()
    {
        var store = new AppSettingsStore(FilePath);

        await store.SaveAsync(new AppSettings { DriveLetter = "K" });
        var loaded = await store.LoadAsync();

        Assert.Equal("K", loaded.DriveLetter);
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public async Task WhenFileIsCorruptThenDefaultsAreReturnedAndFileIsMovedAside()
    {
        Directory.CreateDirectory(_tempDir);
        await File.WriteAllTextAsync(FilePath, "{ \"driveLetter\": ");
        var store = new AppSettingsStore(FilePath);

        var loaded = await store.LoadAsync();

        Assert.Equal(new AppSettings().DriveLetter, loaded.DriveLetter);
        Assert.False(File.Exists(FilePath));
        Assert.Single(Directory.GetFiles(_tempDir, "settings.json.corrupt-*"));
    }
}
