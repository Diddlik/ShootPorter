using System.Text.Json;
using Serilog;

namespace ShootPorter.Core.Profiles;

/// <summary>
/// Persists application settings to a JSON file on disk.
/// Writes go to a temp file first and replace the real file atomically, so an interrupted save never
/// leaves a truncated file. An unreadable file is moved aside instead of being overwritten by defaults.
/// </summary>
public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public AppSettingsStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken)
                       .ConfigureAwait(false)
                   ?? new AppSettings();
        }
        catch (JsonException ex)
        {
            var backupPath = $"{_filePath}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
            Log.Error(ex, "Settings file is unreadable; moved to {BackupPath} and using defaults", backupPath);
            File.Move(_filePath, backupPath, overwrite: true);
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tempPath = PrepareTempPath();
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, CancellationToken.None).ConfigureAwait(false);
            }

            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Synchronous save for application shutdown, where an async save may not finish before the process exits.
    /// </summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _writeLock.Wait();
        try
        {
            var tempPath = PrepareTempPath();
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private string PrepareTempPath()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        return _filePath + ".tmp";
    }
}
