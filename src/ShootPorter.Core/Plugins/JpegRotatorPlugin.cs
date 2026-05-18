namespace ShootPorter.Core.Plugins;

/// <summary>
/// Auto-rotates JPEG files based on EXIF orientation tag.
/// Currently a stub — full implementation requires an image processing library.
/// </summary>
public sealed class JpegRotatorPlugin : IPostDownloadPlugin
{
    public string Name => "JPEG Auto-Rotate";
    public int Order => 30;
    public bool IsEnabled { get; set; } = true;

    public Task<bool> ProcessAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        var ext = Path.GetExtension(filePath);
        if (!ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(true); // not a JPEG, skip silently
        }

        // TODO: Read EXIF orientation, rotate pixel data, reset orientation tag
        return Task.FromResult(true);
    }
}
