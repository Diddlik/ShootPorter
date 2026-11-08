using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.QuickTime;
using MetadataExtractor.Formats.Xmp;
using Serilog;
using ShootPorter.Core.Properties;

namespace ShootPorter.Core.Metadata;

/// <summary>
/// Finds the capture time of a video as the camera's wall clock, from the most reliable source the file offers.
/// Order: Sony clip XML (sidecar, then embedded in the MP4), QuickTime <c>creationdate</c> key, XMP,
/// then the movie header with a time zone derived from the file time, then the header with the user setting.
/// </summary>
internal static partial class VideoCaptureDate
{
    private const long MaxBoxBytes = 64L * 1024 * 1024;
    private static readonly TimeSpan OffsetStep = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxOffset = TimeSpan.FromHours(14);
    private static readonly TimeSpan MaxWriteDelay = TimeSpan.FromSeconds(90);

    [GeneratedRegex("<CreationDate\\s+value=\"([^\"]+)\"")]
    private static partial Regex CreationDateRegex();

    public static DateTimeOffset? Resolve(
        string filePath, IReadOnlyList<MetadataExtractor.Directory> directories,
        QuickTimeMovieHeaderDirectory movieHeader, bool headerIsLocal)
    {
        return FromSonySidecar(filePath)
            ?? FromSonyEmbeddedXml(filePath)
            ?? FromQuickTimeKeys(directories)
            ?? FromXmp(directories)
            ?? FromMovieHeader(filePath, movieHeader, headerIsLocal);
    }

    // Photos are named by the camera clock; videos use the same, so one shoot sorts together.
    private static DateTimeOffset WallClock(DateTime time) =>
        new(DateTime.SpecifyKind(time, DateTimeKind.Unspecified));

    private static DateTimeOffset? ParseWallClock(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? WallClock(date.DateTime)
            : null;

    private static DateTimeOffset? ParseCreationDate(string text)
    {
        var match = CreationDateRegex().Match(text);
        return match.Success ? ParseWallClock(match.Groups[1].Value) : null;
    }

    // Sony: C0001.MP4 -> C0001M01.XML holds the camera clock with its offset.
    private static DateTimeOffset? FromSonySidecar(string filePath)
    {
        var sidecar = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(filePath) + "M01.XML");
        if (!File.Exists(sidecar))
            return null;

        try
        {
            return ParseCreationDate(File.ReadAllText(sidecar));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, Resources.LogReadingTheVideoDateFailed, sidecar);
            return null;
        }
    }

    // Sony also stores the same XML in a top-level box of the MP4 (a meta box after moov).
    // Only box headers are read and the media data (mdat) is skipped, so large clips are not scanned.
    private static DateTimeOffset? FromSonyEmbeddedXml(string filePath)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
            var header = new byte[16];
            long position = 0;
            while (position + 8 <= stream.Length)
            {
                stream.Position = position;
                stream.ReadExactly(header, 0, 8);
                long size = BinaryPrimitives.ReadUInt32BigEndian(header);
                var type = Encoding.ASCII.GetString(header, 4, 4);
                var headerLength = 8;
                if (size == 1)
                {
                    stream.ReadExactly(header, 8, 8);
                    size = (long)BinaryPrimitives.ReadUInt64BigEndian(header.AsSpan(8));
                    headerLength = 16;
                }
                else if (size == 0)
                {
                    size = stream.Length - position;
                }

                if (size < headerLength)
                    return null;

                if (type != "mdat" && size <= MaxBoxBytes)
                {
                    var box = new byte[size - headerLength];
                    stream.ReadExactly(box);
                    if (ParseCreationDate(Encoding.Latin1.GetString(box)) is { } date)
                        return date;
                }

                position += size;
            }

            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, Resources.LogReadingTheVideoDateFailed, filePath);
            return null;
        }
    }

    // iPhones and many Android phones write com.apple.quicktime.creationdate with the local offset.
    private static DateTimeOffset? FromQuickTimeKeys(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var keys = directories.OfType<QuickTimeMetadataHeaderDirectory>().FirstOrDefault();
        return keys?.GetObject(QuickTimeMetadataHeaderDirectory.TagCreationDate) switch
        {
            DateTimeOffset date => WallClock(date.DateTime),
            DateTime { Kind: DateTimeKind.Utc } date => WallClock(date.ToLocalTime()),
            DateTime date => WallClock(date),
            string text => ParseWallClock(text),
            _ => null,
        };
    }

    private static DateTimeOffset? FromXmp(IReadOnlyList<MetadataExtractor.Directory> directories)
    {
        var properties = directories.OfType<XmpDirectory>().FirstOrDefault()?.GetXmpProperties();
        if (properties is null)
            return null;

        return properties.TryGetValue("xmp:CreateDate", out var created) ? ParseWallClock(created)
            : properties.TryGetValue("photoshop:DateCreated", out var dateCreated) ? ParseWallClock(dateCreated)
            : null;
    }

    private static DateTimeOffset? FromMovieHeader(string filePath, QuickTimeMovieHeaderDirectory movieHeader, bool headerIsLocal)
    {
        // QuickTime stores seconds since 1904; 0 means the camera did not set it.
        if (!movieHeader.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out var created) || created.Year < 1971)
            return null;

        var header = DateTime.SpecifyKind(created, DateTimeKind.Unspecified);
        if (DetectCameraOffset(filePath, header, movieHeader) is { } offset)
            return WallClock(header + offset);

        return headerIsLocal
            ? WallClock(header)
            : new DateTimeOffset(DateTime.SpecifyKind(created, DateTimeKind.Utc)).ToLocalTime();
    }

    // The camera sets the file time (its own clock) when recording stops. Start = file time - duration;
    // start - header is the camera's offset to the header (0 if the header is local, the time zone if it is UTC).
    // A copied or edited file has an unrelated file time, which rarely lands on a 15-minute step.
    private static TimeSpan? DetectCameraOffset(string filePath, DateTime header, QuickTimeMovieHeaderDirectory movieHeader)
    {
        var duration = movieHeader.GetObject(QuickTimeMovieHeaderDirectory.TagDuration) as TimeSpan? ?? TimeSpan.Zero;
        var started = File.GetLastWriteTime(filePath) - duration;
        var raw = started - header;
        var rounded = TimeSpan.FromTicks((long)Math.Round((double)raw.Ticks / OffsetStep.Ticks) * OffsetStep.Ticks);
        return (raw - rounded).Duration() <= MaxWriteDelay && rounded.Duration() <= MaxOffset ? rounded : null;
    }
}
