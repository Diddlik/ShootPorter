using ShootPorter.Core.Profiles;

namespace ShootPorter.Core.Tests.Profiles;

/// <summary>
/// Tests for <see cref="CameraAliasStore"/> covering alias resolution and persistence.
/// </summary>
public sealed class CameraAliasStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public CameraAliasStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "aliases.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static CameraAlias MakeAlias(string identifier, string friendly) => new()
    {
        Identifier = identifier,
        FriendlyName = friendly,
    };

    [Fact]
    public async Task WhenLoadingNonExistentFileThenStartsEmpty()
    {
        var store = new CameraAliasStore(Path.Combine(_tempDir, "missing.json"));

        await store.LoadAsync();

        Assert.Empty(store.Aliases);
    }

    [Fact]
    public void WhenResolvingUnknownIdentifierThenReturnsOriginal()
    {
        var store = new CameraAliasStore(_filePath);
        const string unknown = "Canon EOS R5 Unknown";

        var result = store.Resolve(unknown);

        Assert.Equal(unknown, result);
    }

    [Fact]
    public void WhenResolvingKnownAliasThenReturnsFriendlyName()
    {
        var store = new CameraAliasStore(_filePath);
        store.AddOrUpdate(MakeAlias("SN-123456", "My Sony A7IV"));

        var result = store.Resolve("SN-123456");

        Assert.Equal("My Sony A7IV", result);
    }

    [Fact]
    public async Task WhenSavingAndLoadingThenRoundTrips()
    {
        var store = new CameraAliasStore(_filePath);
        store.AddOrUpdate(MakeAlias("Canon EOS R5", "Primary Body"));
        store.AddOrUpdate(MakeAlias("Nikon Z9", "Backup Body"));
        await store.SaveAsync();

        var store2 = new CameraAliasStore(_filePath);
        await store2.LoadAsync();

        Assert.Equal(2, store2.Aliases.Count);
        Assert.Equal("Primary Body", store2.Resolve("Canon EOS R5"));
        Assert.Equal("Backup Body", store2.Resolve("Nikon Z9"));
    }

    [Fact]
    public void WhenUpdatingAliasThenOverwritesPrevious()
    {
        var store = new CameraAliasStore(_filePath);
        store.AddOrUpdate(MakeAlias("SN-789", "Old Name"));

        store.AddOrUpdate(MakeAlias("SN-789", "New Name"));

        Assert.Single(store.Aliases);
        Assert.Equal("New Name", store.Resolve("SN-789"));
    }

    [Fact]
    public void WhenRemovingAliasThenResolvesBackToOriginal()
    {
        var store = new CameraAliasStore(_filePath);
        store.AddOrUpdate(MakeAlias("MODEL-X", "X Camera"));

        var removed = store.Remove("MODEL-X");

        Assert.True(removed);
        Assert.Equal("MODEL-X", store.Resolve("MODEL-X"));
    }

    [Fact]
    public void WhenResolvingIsCaseInsensitiveThenMatchesAlias()
    {
        var store = new CameraAliasStore(_filePath);
        store.AddOrUpdate(MakeAlias("sony-a7iv", "Sony A7 IV"));

        var result = store.Resolve("SONY-A7IV");

        Assert.Equal("Sony A7 IV", result);
    }
}
