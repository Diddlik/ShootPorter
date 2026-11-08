using System;
using System.Threading;
using System.Threading.Tasks;
using Velopack;

namespace ShootPorter.App.Services;

/// <summary>
/// Checks GitHub Releases for application updates and manages the download/apply lifecycle.
/// </summary>
public sealed class UpdateService
{
    // The release download URL is served without the GitHub API, so it has no rate limit.
    private const string FeedUrl = "https://github.com/Diddlik/ShootPorter/releases/latest/download/";

    private readonly UpdateManager _updateManager;
    private UpdateInfo? _pendingUpdate;

    public UpdateService()
    {
        _updateManager = new UpdateManager(FeedUrl);
    }

    /// <summary>
    /// Returns the current application version string, or "dev" if not installed via Velopack.
    /// </summary>
    public string CurrentVersion =>
        _updateManager.IsInstalled
            ? _updateManager.CurrentVersion?.ToString() ?? "unknown"
            : "dev";

    /// <summary>
    /// Whether the app was installed via Velopack (vs running from IDE/debug).
    /// Update checks are skipped when not installed.
    /// </summary>
    public bool IsInstalled => _updateManager.IsInstalled;

    /// <summary>
    /// Checks GitHub for a newer release. Returns version string if available, null otherwise.
    /// </summary>
    public async Task<string?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        if (!_updateManager.IsInstalled)
            return null;

        _pendingUpdate = await _updateManager.CheckForUpdatesAsync().ConfigureAwait(false);
        return _pendingUpdate?.TargetFullRelease?.Version?.ToString();
    }

    /// <summary>
    /// Downloads the pending update with progress reporting (0-100).
    /// </summary>
    public async Task DownloadUpdateAsync(
        Action<int>? progressCallback = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(_pendingUpdate);

        await _updateManager.DownloadUpdatesAsync(
            _pendingUpdate,
            progress => progressCallback?.Invoke(progress),
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies the downloaded update and restarts the application.
    /// This method does not return — the process exits.
    /// </summary>
    public void ApplyUpdateAndRestart()
    {
        ArgumentNullException.ThrowIfNull(_pendingUpdate);
        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate.TargetFullRelease);
    }

    /// <summary>
    /// Applies the downloaded update without restarting.
    /// The update takes effect on the next app launch.
    /// </summary>
    public void ApplyUpdateOnExit()
    {
        if (_pendingUpdate is not null)
            _updateManager.ApplyUpdatesAndExit(_pendingUpdate.TargetFullRelease);
    }
}
