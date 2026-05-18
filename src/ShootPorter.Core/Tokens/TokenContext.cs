using System.Text.RegularExpressions;

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
