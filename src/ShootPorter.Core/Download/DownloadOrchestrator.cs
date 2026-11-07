using System.Collections.Concurrent;
using System.Diagnostics;
using ShootPorter.Core.Dedup;
using ShootPorter.Core.Tokens;
using Serilog;

namespace ShootPorter.Core.Download;

/// <summary>
/// Orchestrates parallel file downloads from source to destination with progress reporting and backup support.
/// Existing destination files are only replaced when <see cref="DownloadOptions.ResolveExistingFile"/> says so.
/// </summary>
public sealed class DownloadOrchestrator
{
    private static readonly string[] SidecarExtensions = [".xmp", ".wav", ".thm"];

    private readonly FileCopier _copier = new();

    /// <summary>
    /// Downloads a batch of files with configurable parallelism and progress reporting.
    /// Each item is a (sourcePath, destinationPath) pair. <paramref name="fileCompleted"/> receives each
    /// file's result as soon as it is final, so callers keep finished results even when the batch is cancelled.
    /// </summary>
    public async Task<DownloadBatchResult> DownloadBatchAsync(
        IReadOnlyList<(string Source, string Destination)> filePairs,
        DownloadOptions options,
        IProgress<DownloadProgress>? progress = null,
        IProgress<DownloadResult>? fileCompleted = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePairs);
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();
        var results = new ConcurrentBag<DownloadResult>();
        var claimedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sidecarIndex = new ConcurrentDictionary<string, ILookup<string, string>>(StringComparer.OrdinalIgnoreCase);
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
                var result = await DownloadFileAsync(
                    source, destination, index + 1, totalFiles, options, claimedPaths, sidecarIndex, progress, ct).ConfigureAwait(false);

                results.Add(result);
                fileCompleted?.Report(result);
            }).ConfigureAwait(false);

        stopwatch.Stop();

        var resultList = results.ToList();
        var succeeded = resultList.Count(r => r.Success);
        var failed = resultList.Count(r => !r.Success);
        var totalBytes = resultList.Where(r => r.Success).Sum(r => r.BytesCopied);

        return new DownloadBatchResult(resultList, succeeded, failed, totalBytes, stopwatch.Elapsed);
    }

    private async Task<DownloadResult> DownloadFileAsync(
        string source,
        string destination,
        int fileIndex,
        int totalFiles,
        DownloadOptions options,
        HashSet<string> claimedPaths,
        ConcurrentDictionary<string, ILookup<string, string>> sidecarIndex,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        if (!File.Exists(source))
            return new DownloadResult(source, destination, false, 0, $"Source file not found: {source}");

        var (target, overwrite) = await ResolveTargetAsync(destination, claimedPaths, options, ct).ConfigureAwait(false);
        var result = await _copier.CopyFileAsync(
            source, target, options.BufferSize, options.VerifyAfterCopy,
            progress, fileIndex, totalFiles, overwrite, ct).ConfigureAwait(false);

        if (!result.Success)
            return result;

        var sidecars = new List<(string Source, List<string> Copies)>();
        foreach (var sidecar in FindSidecars(source, sidecarIndex))
        {
            var requestedSidecarTarget = SidecarPath(target, source, sidecar);
            if (!TryClaim(requestedSidecarTarget, claimedPaths))
                continue; // already copied together with a file of the same base name

            var (sidecarTarget, overwriteSidecar) = await ResolveSidecarTargetAsync(
                requestedSidecarTarget, overwrite, claimedPaths, options, ct).ConfigureAwait(false);
            var sidecarResult = await _copier.CopyFileAsync(
                sidecar, sidecarTarget, options.BufferSize, options.VerifyAfterCopy,
                null, fileIndex, totalFiles, overwriteSidecar, ct).ConfigureAwait(false);
            if (!sidecarResult.Success)
                return RetainSource(result, options, $"Sidecar copy failed for '{sidecarTarget}': {sidecarResult.ErrorMessage}",
                    "Source retained because a sidecar copy failed.");

            sidecars.Add((sidecar, [sidecarTarget]));
        }

        var backupPaths = new List<string>(options.BackupDestinations.Count);
        foreach (var backupRoot in options.BackupDestinations)
        {
            var requestedBackupPath = RebuildPathUnderRoot(target, backupRoot, options.DestinationRoot);
            var (backupPath, overwriteBackup) = await ResolveTargetAsync(
                requestedBackupPath, claimedPaths, options, ct).ConfigureAwait(false);
            // Copy from the primary copy: the card is usually the slowest device involved.
            var backupResult = await _copier.CopyFileAsync(
                target, backupPath, options.BufferSize, options.VerifyAfterCopy,
                null, fileIndex, totalFiles, overwriteBackup, ct).ConfigureAwait(false);

            if (!backupResult.Success)
                return RetainSource(result, options, $"Backup failed for '{backupPath}': {backupResult.ErrorMessage}",
                    "Source retained because a backup copy failed.");

            foreach (var (sidecarSource, copies) in sidecars)
            {
                var requestedSidecarBackup = SidecarPath(backupPath, source, sidecarSource);
                TryClaim(requestedSidecarBackup, claimedPaths);
                var (sidecarBackup, overwriteSidecar) = await ResolveSidecarTargetAsync(
                    requestedSidecarBackup, overwriteBackup, claimedPaths, options, ct).ConfigureAwait(false);
                var sidecarResult = await _copier.CopyFileAsync(
                    copies[0], sidecarBackup, options.BufferSize, options.VerifyAfterCopy,
                    null, fileIndex, totalFiles, overwriteSidecar, ct).ConfigureAwait(false);
                if (!sidecarResult.Success)
                    return RetainSource(result, options, $"Backup failed for '{sidecarBackup}': {sidecarResult.ErrorMessage}",
                        "Source retained because a backup copy failed.");

                copies.Add(sidecarBackup);
            }

            backupPaths.Add(backupPath);
        }

        return await ApplySourceDeletionAsync(result, backupPaths, sidecars, options, ct).ConfigureAwait(false);
    }

    private static DownloadResult RetainSource(DownloadResult result, DownloadOptions options, string error, string reason) =>
        result with
        {
            Success = false,
            ErrorMessage = error,
            SourceDisposition = options.AutoDeleteSource
                ? SourceFileDisposition.Retained
                : SourceFileDisposition.NotRequested,
            SourceDispositionMessage = options.AutoDeleteSource ? reason : null,
        };

    /// <summary>
    /// Finds sidecars next to <paramref name="source"/>: "IMG_1.xmp" and "IMG_1.CR3.xmp" both belong to "IMG_1.CR3".
    /// Each directory is listed once per batch.
    /// </summary>
    private static IEnumerable<string> FindSidecars(
        string source, ConcurrentDictionary<string, ILookup<string, string>> sidecarIndex)
    {
        var byStem = sidecarIndex.GetOrAdd(Path.GetDirectoryName(source)!, dir => Directory.EnumerateFiles(dir)
            .Where(f => SidecarExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .ToLookup(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase));

        return byStem[Path.GetFileNameWithoutExtension(source)].Concat(byStem[Path.GetFileName(source)]);
    }

    // The sidecar keeps whatever follows the source's base name (".xmp" or ".CR3.xmp") and takes the target's base name.
    private static string SidecarPath(string target, string source, string sidecar)
    {
        var suffix = Path.GetFileName(sidecar)[Path.GetFileNameWithoutExtension(source).Length..];
        return Path.Combine(Path.GetDirectoryName(target)!, Path.GetFileNameWithoutExtension(target) + suffix);
    }

    private static bool TryClaim(string path, HashSet<string> claimedPaths)
    {
        lock (claimedPaths)
            return claimedPaths.Add(path);
    }

    /// <summary>
    /// A sidecar follows its main file: replaced when the main file was replaced. Otherwise an existing
    /// sidecar on disk is resolved by <see cref="DownloadOptions.ResolveExistingFile"/> like a main file.
    /// The requested path must already be claimed.
    /// </summary>
    private static async Task<(string Path, bool Overwrite)> ResolveSidecarTargetAsync(
        string requested, bool mainOverwritten, HashSet<string> claimedPaths, DownloadOptions options, CancellationToken ct)
    {
        if (mainOverwritten || !File.Exists(requested))
            return (requested, mainOverwritten);

        if (options.ResolveExistingFile is not null &&
            await options.ResolveExistingFile(requested, ct).ConfigureAwait(false) == ConflictResolution.Overwrite)
            return (requested, true);

        lock (claimedPaths)
        {
            var unique = TokenParser.MakeUnique(requested, p => claimedPaths.Contains(p) || File.Exists(p));
            claimedPaths.Add(unique);
            return (unique, false);
        }
    }

    /// <summary>
    /// Picks the path a file is written to. A path already written in this batch always gets a unique suffix;
    /// a path that exists on disk is resolved by <see cref="DownloadOptions.ResolveExistingFile"/>.
    /// </summary>
    private static async Task<(string Path, bool Overwrite)> ResolveTargetAsync(
        string destination, HashSet<string> claimedPaths, DownloadOptions options, CancellationToken ct)
    {
        bool claimedInBatch;
        lock (claimedPaths)
        {
            claimedInBatch = claimedPaths.Contains(destination);
            if (!claimedInBatch && !File.Exists(destination))
            {
                claimedPaths.Add(destination);
                return (destination, false);
            }
        }

        if (!claimedInBatch && options.ResolveExistingFile is not null &&
            await options.ResolveExistingFile(destination, ct).ConfigureAwait(false) == ConflictResolution.Overwrite)
        {
            lock (claimedPaths)
                claimedPaths.Add(destination);
            return (destination, true);
        }

        lock (claimedPaths)
        {
            var unique = TokenParser.MakeUnique(destination, p => claimedPaths.Contains(p) || File.Exists(p));
            claimedPaths.Add(unique);
            return (unique, false);
        }
    }

    private static async Task<DownloadResult> ApplySourceDeletionAsync(
        DownloadResult result,
        IReadOnlyList<string> backupPaths,
        IReadOnlyList<(string Source, List<string> Copies)> sidecars,
        DownloadOptions options,
        CancellationToken ct)
    {
        if (!options.AutoDeleteSource)
            return result;

        if (!options.VerifyAfterCopy)
        {
            return result with
            {
                SourceDisposition = SourceFileDisposition.Retained,
                SourceDispositionMessage = "Source retained because transfer verification is disabled.",
            };
        }

        var toCheck = sidecars.Prepend((result.SourcePath, backupPaths.Prepend(result.DestinationPath).ToList()));
        foreach (var (sourcePath, copies) in toCheck)
        {
            var sourceHash = await FileComparer.ComputeHashAsync(sourcePath, ct).ConfigureAwait(false);
            foreach (var copyPath in copies)
            {
                var copyHash = await FileComparer.ComputeHashAsync(copyPath, ct).ConfigureAwait(false);
                if (!sourceHash.AsSpan().SequenceEqual(copyHash))
                {
                    return result with
                    {
                        Success = false,
                        ErrorMessage = $"Content check failed for '{copyPath}': copy does not match the source.",
                        SourceDisposition = SourceFileDisposition.Retained,
                        SourceDispositionMessage = "Source retained because a copy does not match the source content.",
                    };
                }
            }
        }

        try
        {
            File.Delete(result.SourcePath);
            foreach (var (sidecarSource, _) in sidecars)
                File.Delete(sidecarSource);
            return result with
            {
                SourceDisposition = SourceFileDisposition.Deleted,
                SourceDispositionMessage = "Source deleted after verified transfer.",
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error(ex, "Deleting source {SourcePath} failed after a verified copy", result.SourcePath);
            return result with
            {
                Success = false,
                ErrorMessage = $"Source delete failed after successful copy: {ex.Message}",
                SourceDisposition = SourceFileDisposition.DeleteFailed,
                SourceDispositionMessage = "Destination files were kept; source could not be deleted.",
            };
        }
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
