namespace ShootPorter.Core.Plugins;

/// <summary>
/// Interface for plugins that execute after a file has been downloaded to its destination.
/// </summary>
public interface IPostDownloadPlugin
{
    /// <summary>Plugin display name.</summary>
    string Name { get; }

    /// <summary>Execution order — lower values run first.</summary>
    int Order { get; }

    /// <summary>Whether this plugin is currently enabled.</summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Processes a single downloaded file.
    /// </summary>
    /// <param name="filePath">Full path to the downloaded file at its destination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if processing succeeded.</returns>
    Task<bool> ProcessAsync(string filePath, CancellationToken cancellationToken = default);
}
