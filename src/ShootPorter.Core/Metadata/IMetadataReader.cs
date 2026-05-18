namespace ShootPorter.Core.Metadata;

/// <summary>
/// Abstracts metadata extraction to support testing without real image files.
/// </summary>
public interface IMetadataReader
{
    Task<FileMetadata> ReadMetadataAsync(string filePath, CancellationToken cancellationToken = default);
}
