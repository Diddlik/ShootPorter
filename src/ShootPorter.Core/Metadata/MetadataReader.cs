using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.QuickTime;
using Serilog;
using ShootPorter.Core.Properties;

namespace ShootPorter.Core.Metadata;

/// <summary>
/// Reads EXIF, IPTC, and XMP metadata from image and video files using the MetadataExtractor library.
/// </summary>
/// <param name="videoTimeIsLocal">
/// Treat the QuickTime movie header date as local time. The spec (and phones) use UTC,
/// but many cameras write local time there.
/// </param>
public sealed class MetadataReader(bool videoTimeIsLocal = false) : IMetadataReader
{
    public Task<FileMetadata> ReadMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        // MetadataExtractor is synchronous, so wrap in Task.Run
        return Task.Run(() => ReadMetadataCore(filePath), cancellationToken);
    }

    private FileMetadata ReadMetadataCore(string filePath)
    {
        var directories = ImageMetadataReader.ReadMetadata(filePath);

        var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        var gpsDir = directories.OfType<GpsDirectory>().FirstOrDefault();
        var iptcDir = directories.OfType<IptcDirectory>().FirstOrDefault();
        var movieHeader = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();

        return new FileMetadata
        {
            FilePath = filePath,
            CaptureDateTime = ExtractDateTime(exifSubIfd, exifIfd0) ?? ReadClipSidecarDate(filePath) ?? ExtractVideoDateTime(movieHeader),
            CameraManufacturer = exifIfd0?.GetDescription(ExifDirectoryBase.TagMake)?.Trim(),
            CameraModel = exifIfd0?.GetDescription(ExifDirectoryBase.TagModel)?.Trim(),
            CameraSerialNumber = (exifSubIfd?.GetDescription(ExifDirectoryBase.TagBodySerialNumber)
                                  ?? exifIfd0?.GetDescription(ExifDirectoryBase.TagBodySerialNumber))?.Trim(),
            IsoSpeed = GetIsoSpeed(exifSubIfd),
            Aperture = exifSubIfd?.GetDescription(ExifDirectoryBase.TagFNumber),
            ShutterSpeed = exifSubIfd?.GetDescription(ExifDirectoryBase.TagExposureTime),
            ImageWidth = GetIntTag(exifSubIfd, ExifDirectoryBase.TagExifImageWidth)
                      ?? GetIntTag(exifIfd0, ExifDirectoryBase.TagImageWidth),
            ImageHeight = GetIntTag(exifSubIfd, ExifDirectoryBase.TagExifImageHeight)
                       ?? GetIntTag(exifIfd0, ExifDirectoryBase.TagImageHeight),
            GpsLatitude = gpsDir?.GetGeoLocation()?.Latitude,
            GpsLongitude = gpsDir?.GetGeoLocation()?.Longitude,
            GpsAltitude = GetGpsAltitude(gpsDir),
            Copyright = exifIfd0?.GetDescription(ExifDirectoryBase.TagCopyright)?.Trim(),
            Artist = exifIfd0?.GetDescription(ExifDirectoryBase.TagArtist)?.Trim(),
            Keywords = ExtractKeywords(iptcDir),
        };
    }

    private static DateTimeOffset? ExtractDateTime(ExifSubIfdDirectory? subIfd, ExifIfd0Directory? ifd0)
    {
        // Prefer DateTimeOriginal from SubIFD, fall back to DateTime from IFD0
        if (subIfd?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dtOriginal) == true)
            return new DateTimeOffset(dtOriginal);

        if (ifd0?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dt) == true)
            return new DateTimeOffset(dt);

        return null;
    }

    // Sony writes UTC into the movie header; the clip sidecar (C0001.MP4 -> C0001M01.XML) holds the camera clock.
    // The wall-clock part is used, like for photos, so videos and photos of one shoot sort together.
    private static DateTimeOffset? ReadClipSidecarDate(string filePath)
    {
        var sidecar = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(filePath) + "M01.XML");
        if (!File.Exists(sidecar))
            return null;

        try
        {
            var value = XDocument.Load(sidecar).Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "CreationDate")?.Attribute("value")?.Value;
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? new DateTimeOffset(date.DateTime)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
        {
            Log.Warning(ex, Resources.LogReadingClipSidecarFailed, sidecar);
            return null;
        }
    }

    private DateTimeOffset? ExtractVideoDateTime(QuickTimeMovieHeaderDirectory? movieHeader)
    {
        // QuickTime stores seconds since 1904; 0 means the camera did not set it.
        if (movieHeader?.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out var created) != true
            || created.Year < 1971)
            return null;

        return videoTimeIsLocal
            ? new DateTimeOffset(DateTime.SpecifyKind(created, DateTimeKind.Unspecified))
            : new DateTimeOffset(DateTime.SpecifyKind(created, DateTimeKind.Utc)).ToLocalTime();
    }

    private static int? GetIsoSpeed(ExifSubIfdDirectory? subIfd)
    {
        if (subIfd is null) return null;
        return subIfd.TryGetInt32(ExifDirectoryBase.TagIsoEquivalent, out var iso) && iso > 0 ? iso : null;
    }

    private static int? GetIntTag(MetadataExtractor.Directory? directory, int tagType)
    {
        if (directory is null) return null;
        return directory.TryGetInt32(tagType, out var value) ? value : null;
    }

    private static double? GetGpsAltitude(GpsDirectory? gpsDir)
    {
        if (gpsDir is null) return null;
        if (!gpsDir.TryGetRational(GpsDirectory.TagAltitude, out var altitude)) return null;

        var altitudeValue = altitude.ToDouble();
        // Altitude ref byte value 1 means below sea level
        if (gpsDir.TryGetByte(GpsDirectory.TagAltitudeRef, out var altRef) && altRef == 1)
            altitudeValue = -altitudeValue;

        return altitudeValue;
    }

    private static IReadOnlyList<string> ExtractKeywords(IptcDirectory? iptcDir)
    {
        if (iptcDir is null) return [];

        var keywords = iptcDir.GetStringArray(IptcDirectory.TagKeywords);
        return keywords ?? [];
    }
}
