using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;

namespace ShootPorter.Core.Metadata;

/// <summary>
/// Reads EXIF, IPTC, and XMP metadata from image and video files using the MetadataExtractor library.
/// </summary>
public sealed class MetadataReader : IMetadataReader
{
    public Task<FileMetadata> ReadMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        // MetadataExtractor is synchronous, so wrap in Task.Run
        return Task.Run(() => ReadMetadataCore(filePath), cancellationToken);
    }

    private static FileMetadata ReadMetadataCore(string filePath)
    {
        var directories = ImageMetadataReader.ReadMetadata(filePath);

        var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        var gpsDir = directories.OfType<GpsDirectory>().FirstOrDefault();
        var iptcDir = directories.OfType<IptcDirectory>().FirstOrDefault();

        return new FileMetadata
        {
            FilePath = filePath,
            CaptureDateTime = ExtractDateTime(exifSubIfd, exifIfd0),
            CameraManufacturer = exifIfd0?.GetDescription(ExifDirectoryBase.TagMake)?.Trim(),
            CameraModel = exifIfd0?.GetDescription(ExifDirectoryBase.TagModel)?.Trim(),
            CameraSerialNumber = exifIfd0?.GetDescription(ExifDirectoryBase.TagBodySerialNumber)?.Trim(),
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
