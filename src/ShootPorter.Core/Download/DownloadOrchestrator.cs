using System.Collections.Concurrent;
using System.Diagnostics;

namespace ShootPorter.Core.Download;

/// <summary>
/// Orchestrates parallel file downloads from source to destination with progress reporting and backup support.
/// </summary>
public sealed class DownloadOrchestrator
{
    private readonly FileCopier _copier = new();

    /// <summary>
    /// Downloads a batch of files with configurable parallelism and progress reporting.
    /// Each item is a (sourcePath, destinationPath) pair.
    /// </summary>
    public async Task<DownloadBatchResult> DownloadBatchAsync(
        IReadOnlyList<(string Source, string Destination)> filePairs,
        DownloadOptions options,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePairs);
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();
        var results = new ConcurrentBag<DownloadResult>();
        var totalFiles = filePairs.Count;

        await Parallel.ForEachAsync(
            filePairs.Select((pair, index) => (pair, index)),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxParallelism,
                CancellationToken = cancellationToken,
            },
            async (item, ct) =>
            {
                var ((source, destination), index) = item;

                // Copy to primary destination
                var result = await _copier.CopyFileAsync(
                    source, destination, options.BufferSize, options.VerifyAfterCopy,
                    progress, index + 1, totalFiles, ct).ConfigureAwait(false);

                results.Add(result);

                // Copy to backup destinations if primary succeeded
                if (result.Success)
                {
                    foreach (var backupRoot in options.BackupDestinations)
                    {
                        var backupPath = RebuildPathUnderRoot(destination, backupRoot, options.DestinationRoot);
                        var backupResult = await _copier.CopyFileAsync(
                            source, backupPath, options.BufferSize, options.VerifyAfterCopy,
                            null, index + 1, totalFiles, ct).ConfigureAwait(false);

                        if (!backupResult.Success)
                        {
                            // Replace the successful result with a failure noting the backup failed
                            results.Add(backupResult with { DestinationPath = $"BACKUP:{backupPath}" });
                        }
                    }
                }
            }).ConfigureAwait(false);

        stopwatch.Stop();

        var resultList = results.ToList();
        var succeeded = resultList.Count(r => r.Success);
        var failed = resultList.Count(r => !r.Success);
        var totalBytes = resultList.Where(r => r.Success).Sum(r => r.BytesCopied);

        return new DownloadBatchResult(resultList, succeeded, failed, totalBytes, stopwatch.Elapsed);
    }

    /// <summary>
    /// Reconstructs a file path under a different root, preserving the path relative to the primary destination root when available.
    /// </summary>
    private static string RebuildPathUnderRoot(string originalPath, string newRoot, string? originalRoot)
    {
        if (!string.IsNullOrWhiteSpace(originalRoot))
        {
            var relativePath = Path.GetRelativePath(originalRoot, originalPath);
            if (!relativePath.StartsWith("..", StringComparison.Ordinal) &&
                !Path.IsPathRooted(relativePath))
            {
                return Path.Combine(newRoot, relativePath);
            }
        }

        return Path.Combine(newRoot, Path.GetFileName(originalPath));
    }
}
