using System.Globalization;
using System.Xml.Linq;

namespace ShootPorter.Core.Geo;

/// <summary>
/// Parses GPX (GPS Exchange Format) files into a chronological list of track points.
/// </summary>
public sealed class GpxParser
{
    private static readonly XNamespace GpxNs = "http://www.topografix.com/GPX/1/1";

    /// <summary>
    /// Parses a GPX file and returns all track points sorted by timestamp.
    /// </summary>
    public async Task<IReadOnlyList<TrackPoint>> ParseFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var xml = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        return ParseXml(xml);
    }

    /// <summary>
    /// Parses GPX XML content and returns all track points sorted by timestamp.
    /// </summary>
    public IReadOnlyList<TrackPoint> ParseXml(string gpxContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gpxContent);

        var doc = XDocument.Parse(gpxContent);
        var points = new List<TrackPoint>();

        // GPX has <trk><trkseg><trkpt lat="..." lon="..."><time>...</time><ele>...</ele></trkpt>
        var trkpts = doc.Descendants(GpxNs + "trkpt");

        // Also try without namespace for compatibility
        if (!trkpts.Any())
            trkpts = doc.Descendants("trkpt");

        foreach (var trkpt in trkpts)
        {
            var latAttr = trkpt.Attribute("lat");
            var lonAttr = trkpt.Attribute("lon");
            var timeEl = trkpt.Element(GpxNs + "time") ?? trkpt.Element("time");

            if (latAttr is null || lonAttr is null || timeEl is null)
                continue;

            if (!double.TryParse(latAttr.Value, CultureInfo.InvariantCulture, out var lat))
                continue;
            if (!double.TryParse(lonAttr.Value, CultureInfo.InvariantCulture, out var lon))
                continue;
            if (!DateTimeOffset.TryParse(timeEl.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time))
                continue;

            var eleEl = trkpt.Element(GpxNs + "ele") ?? trkpt.Element("ele");
            double? altitude = null;
            if (eleEl is not null && double.TryParse(eleEl.Value, CultureInfo.InvariantCulture, out var ele))
                altitude = ele;

            points.Add(new TrackPoint(time, new GeoCoordinate(lat, lon, altitude)));
        }

        points.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        return points;
    }
}
