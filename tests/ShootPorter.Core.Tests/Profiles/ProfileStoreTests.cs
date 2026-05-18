using ShootPorter.Core.Profiles;

namespace ShootPorter.Core.Tests.Profiles;

/// <summary>
/// Tests for <see cref="ProfileStore"/> covering persistence and in-memory CRUD operations.
/// </summary>
public sealed class ProfileStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public ProfileStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "profiles.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static UserProfile MakeProfile(string name) => new()
    {
        Name = name,
        DestinationRoot = @"C:\Photos",
        MaxParallelism = 4,
    };

    [Fact]
    public async Task WhenLoadingNonExistentFileThenStartsEmpty()
    {
        var store = new ProfileStore(Path.Combine(_tempDir, "does-not-exist.json"));

        await store.LoadAsync();

        Assert.Empty(store.Profiles);
    }

    [Fact]
    public async Task WhenSavingAndLoadingThenRoundTrips()
    {
        var store = new ProfileStore(_filePath);
        store.AddOrUpdate(MakeProfile("Wedding"));
        store.AddOrUpdate(MakeProfile("Sports"));
        await store.SaveAsync();

        var store2 = new ProfileStore(_filePath);
        await store2.LoadAsync();

        Assert.Equal(2, store2.Profiles.Count);
        Assert.Contains(store2.Profiles, p => p.Name == "Wedding");
        Assert.Contains(store2.Profiles, p => p.Name == "Sports");
    }

    [Fact]
    public void WhenAddingProfileThenCanRetrieveByName()
    {
        var store = new ProfileStore(_filePath);
        var profile = MakeProfile("Travel");

        store.AddOrUpdate(profile);

        var retrieved = store.GetByName("Travel");
        Assert.NotNull(retrieved);
        Assert.Equal("Travel", retrieved.Name);
    }

    [Fact]
    public void WhenRemovingProfileThenNoLongerAvailable()
    {
        var store = new ProfileStore(_filePath);
        store.AddOrUpdate(MakeProfile("Temporary"));

        var removed = store.Remove("Temporary");

        Assert.True(removed);
        Assert.Null(store.GetByName("Temporary"));
        Assert.Empty(store.Profiles);
    }

    [Fact]
    public void WhenUpdatingProfileThenOverwritesPrevious()
    {
        var store = new ProfileStore(_filePath);
        store.AddOrUpdate(new UserProfile { Name = "Portrait", MaxParallelism = 2 });
        var updated = new UserProfile { Name = "Portrait", MaxParallelism = 8, VerifyAfterCopy = false };

        store.AddOrUpdate(updated);

        Assert.Single(store.Profiles);
        var result = store.GetByName("Portrait");
        Assert.NotNull(result);
        Assert.Equal(8, result.MaxParallelism);
        Assert.False(result.VerifyAfterCopy);
    }

    [Fact]
    public void WhenRemovingNonExistentProfileThenReturnsFalse()
    {
        var store = new ProfileStore(_filePath);

        var removed = store.Remove("Ghost");

        Assert.False(removed);
    }

    [Fact]
    public void WhenGetByNameIsCaseInsensitiveThenFindsProfile()
    {
        var store = new ProfileStore(_filePath);
        store.AddOrUpdate(MakeProfile("Landscape"));

        var result = store.GetByName("LANDSCAPE");

        Assert.NotNull(result);
        Assert.Equal("Landscape", result.Name);
    }
}
