using System.Security.Cryptography;

namespace ShootPorter.Core.Dedup;

/// <summary>
/// Compares source files against destination files to determine download status.
/// A target with the same size counts as downloaded; an optional hash gives a definitive match.
/// </summary>
public sealed class FileComparer
{
    private const int HashBufferSize = 81920; // 80 KB buffer for hashing

    /// <summary>
    /// Compares a source file against its expected destination path.
    /// </summary>
    public FileComparisonResult Compare(string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var sourceInfo = new FileInfo(sourcePath);
        if (!sourceInfo.Exists)
            throw new FileNotFoundException("Source file not found.", sourcePath);

        if (!File.Exists(destinationPath))
        {
            return new FileComparisonResult(sourcePath, destinationPath, FileStatus.New, sourceInfo.Length, null);
        }

        var destInfo = new FileInfo(destinationPath);

        if (sourceInfo.Length != destInfo.Length)
        {
            return new FileComparisonResult(sourcePath, destinationPath, FileStatus.SizeMismatch, sourceInfo.Length, destInfo.Length);
        }

        // Same size is enough: the target name already encodes capture time and file number,
        // and copies made by other tools or over network shares often carry the copy time.
        return new FileComparisonResult(sourcePath, destinationPath, FileStatus.Downloaded, sourceInfo.Length, destInfo.Length);
    }

    /// <summary>
    /// Performs a byte-level hash comparison to definitively determine if two files are identical.
    /// Use when size/time comparison is ambiguous.
    /// </summary>
    public async Task<bool> AreFilesIdenticalAsync(string path1, string path2, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path1);
        ArgumentException.ThrowIfNullOrWhiteSpace(path2);

        var info1 = new FileInfo(path1);
        var info2 = new FileInfo(path2);

        if (info1.Length != info2.Length)
            return false;

        var hash1 = await ComputeHashAsync(path1, cancellationToken).ConfigureAwait(false);
        var hash2 = await ComputeHashAsync(path2, cancellationToken).ConfigureAwait(false);

        return hash1.SequenceEqual(hash2);
    }

    /// <summary>
    /// Computes SHA-256 hash of a file.
    /// </summary>
    internal static async Task<byte[]> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            HashBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
    }
}
