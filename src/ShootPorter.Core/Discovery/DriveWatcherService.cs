namespace ShootPorter.Core.Discovery;

/// <summary>
/// Monitors the system for removable drive insertion and removal by polling DriveInfo at a configurable interval.
/// Cross-platform compatible (no WMI/udev dependency).
/// </summary>
public sealed class DriveWatcherService : IDisposable
{
    private readonly TimeSpan _pollInterval;
    private Timer? _timer;
    private HashSet<string> _knownDrives = [];
    private bool _disposed;

    public event EventHandler<DriveChangedEventArgs>? DriveChanged;

    public DriveWatcherService(TimeSpan? pollInterval = null)
    {
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(3);
    }

    /// <summary>
    /// Begins polling for drive changes.
    /// </summary>
    public void StartWatching()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _knownDrives = GetCurrentRemovableDrives();
        _timer = new Timer(CheckDrives, null, _pollInterval, _pollInterval);
    }

    /// <summary>
    /// Stops polling for drive changes.
    /// </summary>
    public void StopWatching()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopWatching();
    }

    private void CheckDrives(object? state)
    {
        var current = GetCurrentRemovableDrives();

        // Detect insertions
        foreach (var drive in current)
        {
            if (!_knownDrives.Contains(drive))
                DriveChanged?.Invoke(this, new DriveChangedEventArgs(drive, IsInserted: true));
        }

        // Detect removals
        foreach (var drive in _knownDrives)
        {
            if (!current.Contains(drive))
                DriveChanged?.Invoke(this, new DriveChangedEventArgs(drive, IsInserted: false));
        }

        _knownDrives = current;
    }

    private static HashSet<string> GetCurrentRemovableDrives()
    {
        try
        {
            return DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                .Select(d => d.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return [];
        }
    }
}
