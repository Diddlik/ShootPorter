using System.Text.Json;

namespace ShootPorter.Core.Profiles;

/// <summary>
/// Persists camera alias mappings to a JSON file on disk.
/// </summary>
public sealed class CameraAliasStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private List<CameraAlias> _aliases = [];

    public CameraAliasStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public IReadOnlyList<CameraAlias> Aliases => _aliases;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _aliases = [];
            return;
        }

        await using var stream = File.OpenRead(_filePath);
        _aliases = await JsonSerializer.DeserializeAsync<List<CameraAlias>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false) ?? [];
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, _aliases, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves a camera identifier to its friendly name. Returns the original identifier if no alias is found.
    /// </summary>
    public string Resolve(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var alias = _aliases.FirstOrDefault(a => a.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase));
        return alias?.FriendlyName ?? identifier;
    }

    /// <summary>
    /// Adds or updates an alias mapping.
    /// </summary>
    public void AddOrUpdate(CameraAlias alias)
    {
        ArgumentNullException.ThrowIfNull(alias);

        var index = _aliases.FindIndex(a => a.Identifier.Equals(alias.Identifier, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            _aliases[index] = alias;
        else
            _aliases.Add(alias);
    }

    /// <summary>
    /// Removes an alias by identifier. Returns true if found and removed.
    /// </summary>
    public bool Remove(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return _aliases.RemoveAll(a => a.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase)) > 0;
    }
}
