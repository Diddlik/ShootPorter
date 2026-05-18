namespace ShootPorter.Core.Plugins;

/// <summary>
/// Creates workflow subdirectories (e.g., "originals", "edited", "exports") alongside downloaded files.
/// </summary>
public sealed class DirectoryMakerPlugin : IPostDownloadPlugin
{
    private readonly IReadOnlyList<string> _subdirectories;

    public DirectoryMakerPlugin(IEnumerable<string>? subdirectories = null)
    {
        _subdirectories = (subdirectories?.ToList()) ?? ["originals", "edited", "exports"];
    }

    public string Name => "Directory Maker";
    public int Order => 10;
    public bool IsEnabled { get; set; } = true;

    public Task<bool> ProcessAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        var parentDir = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(parentDir))
            return Task.FromResult(false);

        foreach (var sub in _subdirectories)
        {
            Directory.CreateDirectory(Path.Combine(parentDir, sub));
        }

        return Task.FromResult(true);
    }
}
