using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShootPorter.Core.Profiles;

/// <summary>
/// Maintains a history of recent job codes with their associated dates, persisted to JSON.
/// Entries older than the retention period are automatically pruned on save.
/// </summary>
public sealed class JobCodeHistory
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly TimeSpan _retentionPeriod;
    private List<JobCodeEntry> _entries = [];

    public JobCodeHistory(string filePath, TimeSpan? retentionPeriod = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
        _retentionPeriod = retentionPeriod ?? TimeSpan.FromDays(60);
    }

    public IReadOnlyList<JobCodeEntry> Entries => _entries;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _entries = [];
            return;
        }

        await using var stream = File.OpenRead(_filePath);
        _entries = await JsonSerializer.DeserializeAsync<List<JobCodeEntry>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false) ?? [];
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        // Prune old entries
        var cutoff = DateTimeOffset.UtcNow - _retentionPeriod;
        _entries.RemoveAll(e => e.UsedDate < cutoff);

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, _entries, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Records a job code usage. Updates the date if the code already exists.
    /// </summary>
    public void Record(string jobCode, DateTimeOffset? date = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobCode);

        var usedDate = date ?? DateTimeOffset.UtcNow;
        var existing = _entries.FindIndex(e => e.Code.Equals(jobCode, StringComparison.OrdinalIgnoreCase));

        if (existing >= 0)
            _entries[existing] = new JobCodeEntry { Code = jobCode, UsedDate = usedDate };
        else
            _entries.Add(new JobCodeEntry { Code = jobCode, UsedDate = usedDate });
    }

    /// <summary>
    /// Returns recent job codes ordered by most recently used.
    /// </summary>
    public IReadOnlyList<string> GetRecent(int maxCount = 20) =>
        _entries.OrderByDescending(e => e.UsedDate).Take(maxCount).Select(e => e.Code).ToList();
}

/// <summary>
/// A single job code entry with its last-used date.
/// </summary>
public sealed record JobCodeEntry
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("usedDate")]
    public required DateTimeOffset UsedDate { get; init; }
}
