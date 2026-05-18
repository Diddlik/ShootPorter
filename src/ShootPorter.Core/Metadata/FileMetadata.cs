namespace ShootPorter.Core.Metadata;

/// <summary>
/// Holds extracted EXIF/IPTC metadata for a single image or video file.
/// </summary>
public sealed record FileMetadata
{
    public required string FilePath { get; init; }
    public DateTimeOffset? CaptureDateTime { get; init; }
    public string? CameraManufacturer { get; init; }
    public string? CameraModel { get; init; }
    public string? CameraSerialNumber { get; init; }
    public int? IsoSpeed { get; init; }
    public string? Aperture { get; init; }
    public string? ShutterSpeed { get; init; }
    public int? ImageWidth { get; init; }
    public int? ImageHeight { get; init; }
    public double? GpsLatitude { get; init; }
    public double? GpsLongitude { get; init; }
    public double? GpsAltitude { get; init; }
    public string? Copyright { get; init; }
    public string? Artist { get; init; }
    public IReadOnlyList<string> Keywords { get; init; } = [];
}
