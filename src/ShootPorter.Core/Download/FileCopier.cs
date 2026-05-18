namespace ShootPorter.Core.Download;

/// <summary>
/// Performs individual file copy operations with progress reporting and integrity verification.
/// </summary>
public sealed class FileCopier
{
    /// <summary>
    /// Copies a single file from source to destination with progress reporting.
    /// Creates destination directory if it doesn't exist.
    /// </summary>
    public async Task<DownloadResult> CopyFileAsync(
        string sourcePath,
        string destinationPath,
        int bufferSize,
        bool verifyAfterCopy,
        IProgress<DownloadProgress>? progress,
        int fileIndex,
        int totalFiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        try
        {
            var sourceInfo = new FileInfo(sourcePath);
            if (!sourceInfo.Exists)
                return new DownloadResult(sourcePath, destinationPath, false, 0, $"Source file not found: {sourcePath}");

            // Create destination directory
            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            long bytesCopied = 0;
            var totalBytes = sourceInfo.Length;

            await using var sourceStream = new FileStream(
                sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

            await using var destStream = new FileStream(
                destinationPath, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
            {
                await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                bytesCopied += bytesRead;

                progress?.Report(new DownloadProgress(
                    sourcePath, destinationPath, bytesCopied, totalBytes, fileIndex, totalFiles));
            }

            await destStream.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Verify after copy
            if (verifyAfterCopy)
            {
                var destInfo = new FileInfo(destinationPath);
                if (destInfo.Length != totalBytes)
                    return new DownloadResult(sourcePath, destinationPath, false, bytesCopied,
                        $"Size mismatch after copy: expected {totalBytes}, got {destInfo.Length}");
            }

            // Preserve last write time
            File.SetLastWriteTimeUtc(destinationPath, sourceInfo.LastWriteTimeUtc);

            return new DownloadResult(sourcePath, destinationPath, true, bytesCopied);
        }
        catch (OperationCanceledException)
        {
            // Clean up partial file on cancellation
            TryDeleteFile(destinationPath);
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDeleteFile(destinationPath);
            return new DownloadResult(sourcePath, destinationPath, false, 0, ex.Message);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); } catch { /* best-effort cleanup */ }
    }
}
