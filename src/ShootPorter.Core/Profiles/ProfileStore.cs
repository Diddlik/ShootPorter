using System.Text.Json;

namespace ShootPorter.Core.Profiles;

/// <summary>
/// Persists user profiles to a JSON file on disk.
/// </summary>
public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private List<UserProfile> _profiles = [];

    public ProfileStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public IReadOnlyList<UserProfile> Profiles => _profiles;

    /// <summary>
    /// Loads profiles from disk. If file doesn't exist, starts with empty list.
    /// </summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _profiles = [];
            return;
        }

        await using var stream = File.OpenRead(_filePath);
        _profiles = await JsonSerializer.DeserializeAsync<List<UserProfile>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false) ?? [];
    }

    /// <summary>
    /// Saves all profiles to disk.
    /// </summary>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, _profiles, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds or updates a profile by name.
    /// </summary>
    public void AddOrUpdate(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var index = _profiles.FindIndex(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            _profiles[index] = profile;
        else
            _profiles.Add(profile);
    }

    /// <summary>
    /// Removes a profile by name. Returns true if found and removed.
    /// </summary>
    public bool Remove(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _profiles.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    /// <summary>
    /// Gets a profile by name. Returns null if not found.
    /// </summary>
    public UserProfile? GetByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
