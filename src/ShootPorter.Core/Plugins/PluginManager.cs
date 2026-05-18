namespace ShootPorter.Core.Plugins;

/// <summary>
/// Manages registration and ordered execution of post-download plugins.
/// </summary>
public sealed class PluginManager
{
    private readonly List<IPostDownloadPlugin> _plugins = [];

    public IReadOnlyList<IPostDownloadPlugin> Plugins => _plugins;

    public void Register(IPostDownloadPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        _plugins.Add(plugin);
        _plugins.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    public void Remove(string pluginName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginName);
        _plugins.RemoveAll(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Runs all enabled plugins in order on the given file.
    /// Returns list of (pluginName, success) results.
    /// </summary>
    public async Task<IReadOnlyList<(string PluginName, bool Success)>> RunAllAsync(
        string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var results = new List<(string, bool)>();

        foreach (var plugin in _plugins)
        {
            if (!plugin.IsEnabled)
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var success = await plugin.ProcessAsync(filePath, cancellationToken).ConfigureAwait(false);
                results.Add((plugin.Name, success));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                results.Add((plugin.Name, false));
            }
        }

        return results;
    }
}
