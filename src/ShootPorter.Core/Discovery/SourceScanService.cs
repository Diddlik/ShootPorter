using System.Runtime.CompilerServices;
using Serilog;
using ShootPorter.Core.Metadata;
using ShootPorter.Core.Properties;

namespace ShootPorter.Core.Discovery;

/// <summary>
/// Scans a source folder recursively and reads metadata, yielding files in chunks.
/// Metadata is read in parallel within a chunk; scan order is preserved.
/// </summary>
public sealed class SourceScanService(IFileSystemScanner scanner, IMetadataReader metadataReader)
{
    public const int DefaultChunkSize = 64;

    public async IAsyncEnumerable<IReadOnlyList<ScannedFile>> ScanAsync(
        string path,
        int chunkSize = DefaultChunkSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkSize, 1);

        var chunk = new List<SourceFile>(chunkSize);
        await foreach (var file in scanner.ScanDirectoryAsync(path, recursive: true, cancellationToken).ConfigureAwait(false))
        {
            chunk.Add(file);
            if (chunk.Count < chunkSize)
                continue;

            yield return await ReadChunkAsync(chunk, cancellationToken).ConfigureAwait(false);
            chunk.Clear();
        }

        if (chunk.Count > 0)
            yield return await ReadChunkAsync(chunk, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ScannedFile>> ReadChunkAsync(List<SourceFile> chunk, CancellationToken ct)
    {
        var metadata = await Task.WhenAll(chunk.Select(f => ReadMetadataOrNullAsync(f.FullPath, ct))).ConfigureAwait(false);
        return chunk.Select((file, i) => new ScannedFile(
            file,
            metadata[i],
            metadata[i]?.CaptureDateTime?.DateTime ?? file.LastModified.LocalDateTime)).ToList();
    }

    private async Task<FileMetadata?> ReadMetadataOrNullAsync(string filePath, CancellationToken ct)
    {
        try
        {
            return await metadataReader.ReadMetadataAsync(filePath, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // MetadataExtractor throws a wide range of exception types on corrupt or unusual files.
            Log.Warning(ex, Resources.LogReadingMetadataFailedFor, filePath);
            return null;
        }
    }
}

/// <summary>
/// A scanned source file with its metadata (null if unreadable) and the capture date used for naming.
/// </summary>
public sealed record ScannedFile(SourceFile File, FileMetadata? Metadata, DateTime CaptureDate);
