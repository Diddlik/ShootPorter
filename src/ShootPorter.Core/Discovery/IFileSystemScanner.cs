namespace ShootPorter.Core.Discovery;

/// <summary>
/// Abstracts file system scanning to allow testing without real disk I/O.
/// </summary>
public interface IFileSystemScanner
{
    IAsyncEnumerable<SourceFile> ScanDirectoryAsync(string path, bool recursive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriveInfo>> GetRemovableDrivesAsync(CancellationToken cancellationToken = default);
}
