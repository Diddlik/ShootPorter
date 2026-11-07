using System.Text.RegularExpressions;
using ShootPorter.Core.Metadata;

namespace ShootPorter.Core.Tokens;

/// <summary>
/// Carries all input data required to resolve tokens for a single file during a download session.
/// </summary>
public sealed partial record TokenContext(
    DateTimeOffset CaptureDateTime,
    string OriginalFileName,
    string Extension,
    string? JobCode,
    int SequenceNumber,
    IReadOnlyDictionary<string, string> CustomTokens)
{
    // File system data
    public string SourceFolderName { get; init; } = string.Empty;
    public string SourceFolderNumber { get; init; } = string.Empty;
    public string ImageNumber { get; init; } = string.Empty;

    // Camera/EXIF data
    public string? CameraManufacturer { get; init; }
    public string? CameraModel { get; init; }
    public string? CameraSerialNumber { get; init; }
    public int? IsoSpeed { get; init; }
    public string? Aperture { get; init; }
    public string? ShutterSpeed { get; init; }
    public string? FocalLength { get; init; }
    public string? Owner { get; init; }
    public string? Copyright { get; init; }
    public int? ImageCounter { get; init; }

    // Session data
    public int SessionSequenceNumber { get; init; } = 1;
    public int DailyDownloadCount { get; init; } = 1;
    public int UniquenessNumber { get; init; }

    // Camera mappings
    public IReadOnlyDictionary<string, string> CameraMappings { get; init; } = new Dictionary<string, string>();

    // Locale for formatting
    public System.Globalization.CultureInfo Culture { get; init; } = System.Globalization.CultureInfo.CurrentCulture;

    /// <summary>
    /// Builds a context for a source file from its path and (optional) metadata.
    /// </summary>
    public static TokenContext ForFile(
        string filePath,
        FileMetadata? metadata,
        DateTimeOffset captureDateTime,
        string? jobCode,
        int sequenceNumber,
        IReadOnlyDictionary<string, string> cameraMappings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(cameraMappings);

        var originalFileName = Path.GetFileNameWithoutExtension(filePath);
        var sourceFolderName = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? string.Empty;

        return new TokenContext(captureDateTime, originalFileName, Path.GetExtension(filePath), jobCode, sequenceNumber,
            new Dictionary<string, string>())
        {
            ImageNumber = ExtractImageNumber(originalFileName),
            SourceFolderName = sourceFolderName,
            SourceFolderNumber = ExtractImageNumber(sourceFolderName),
            CameraManufacturer = metadata?.CameraManufacturer,
            CameraModel = metadata?.CameraModel,
            CameraSerialNumber = metadata?.CameraSerialNumber,
            IsoSpeed = metadata?.IsoSpeed,
            Aperture = metadata?.Aperture,
            ShutterSpeed = metadata?.ShutterSpeed,
            Copyright = metadata?.Copyright,
            Owner = metadata?.Artist,
            CameraMappings = cameraMappings,
        };
    }

    /// <summary>
    /// Extracts the trailing numeric sequence from a filename (e.g. "DSC_0312" → "0312").
    /// </summary>
    public static string ExtractImageNumber(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return string.Empty;
        var matches = DigitSequenceRegex().Matches(fileName);
        return matches.Count > 0 ? matches[^1].Value : string.Empty;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitSequenceRegex();
}
