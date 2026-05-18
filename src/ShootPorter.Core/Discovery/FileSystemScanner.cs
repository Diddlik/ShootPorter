using System.Runtime.CompilerServices;

namespace ShootPorter.Core.Discovery;

/// <summary>
/// Scans directories on the real file system and enumerates removable drives.
/// Files that trigger access-denied errors are skipped rather than propagated.
/// </summary>
public sealed class FileSystemScanner : IFileSystemScanner
{
    /// <inheritdoc/>
    public async IAsyncEnumerable<SourceFile> ScanDirectoryAsync(
        string path,
        bool recursive,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFiles(path, "*", searchOption);
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        await Task.Yield();

        foreach (var filePath in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extension = Path.GetExtension(filePath);
            if (!SupportedFormats.IsSupported(extension))
                continue;

            FileInfo info;
            try
            {
                info = new FileInfo(filePath);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            var normalizedExtension = extension.ToLowerInvariant();
            var category = SupportedFormats.IsImage(normalizedExtension)
                ? FileCategory.Image
                : FileCategory.Video;

            yield return new SourceFile(
                FullPath: filePath,
                FileName: info.Name,
                Extension: normalizedExtension,
                SizeBytes: info.Length,
                LastModified: info.LastWriteTimeUtc,
                Category: category);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<DriveInfo>> GetRemovableDrivesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<DriveInfo> drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
            .ToList();

        return Task.FromResult(drives);
    }
}
