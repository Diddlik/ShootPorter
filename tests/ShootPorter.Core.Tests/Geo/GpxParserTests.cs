using ShootPorter.Core.Geo;

namespace ShootPorter.Core.Tests.Geo;

/// <summary>
/// Tests for <see cref="GpxParser"/> covering XML parsing scenarios.
/// </summary>
public sealed class GpxParserTests
{
    private const string SampleGpx = """
        <?xml version="1.0" encoding="UTF-8"?>
        <gpx xmlns="http://www.topografix.com/GPX/1/1" version="1.1">
          <trk><trkseg>
            <trkpt lat="47.6062" lon="-122.3321"><time>2025-06-15T10:00:00Z</time><ele>56.0</ele></trkpt>
            <trkpt lat="47.6072" lon="-122.3331"><time>2025-06-15T10:05:00Z</time><ele>58.0</ele></trkpt>
            <trkpt lat="47.6082" lon="-122.3341"><time>2025-06-15T10:10:00Z</time><ele>60.0</ele></trkpt>
          </trkseg></trk>
        </gpx>
        """;

    private readonly GpxParser _parser = new();

    [Fact]
    public void WhenParsingValidGpxThenReturnsTrackPoints()
    {
        var points = _parser.ParseXml(SampleGpx);

        Assert.Equal(3, points.Count);
        Assert.Equal(47.6062, points[0].Coordinate.Latitude);
        Assert.Equal(-122.3321, points[0].Coordinate.Longitude);
        Assert.Equal(DateTimeOffset.Parse("2025-06-15T10:00:00Z"), points[0].Timestamp);
    }

    [Fact]
    public void WhenParsingValidGpxThenResultIsSortedByTimestamp()
    {
        // Arrange: reverse order so parser must sort
        const string reversedGpx = """
            <?xml version="1.0" encoding="UTF-8"?>
            <gpx xmlns="http://www.topografix.com/GPX/1/1" version="1.1">
              <trk><trkseg>
                <trkpt lat="47.6082" lon="-122.3341"><time>2025-06-15T10:10:00Z</time></trkpt>
                <trkpt lat="47.6062" lon="-122.3321"><time>2025-06-15T10:00:00Z</time></trkpt>
                <trkpt lat="47.6072" lon="-122.3331"><time>2025-06-15T10:05:00Z</time></trkpt>
              </trkseg></trk>
            </gpx>
            """;

        var points = _parser.ParseXml(reversedGpx);

        Assert.Equal(3, points.Count);
        Assert.True(points[0].Timestamp < points[1].Timestamp);
        Assert.True(points[1].Timestamp < points[2].Timestamp);
    }

    [Fact]
    public void WhenParsingGpxWithAltitudeThenIncludesAltitude()
    {
        var points = _parser.ParseXml(SampleGpx);

        Assert.Equal(56.0, points[0].Coordinate.Altitude);
        Assert.Equal(58.0, points[1].Coordinate.Altitude);
        Assert.Equal(60.0, points[2].Coordinate.Altitude);
    }

    [Fact]
    public void WhenParsingGpxWithoutNamespaceThenStillParses()
    {
        const string noNsGpx = """
            <?xml version="1.0" encoding="UTF-8"?>
            <gpx version="1.1">
              <trk><trkseg>
                <trkpt lat="51.5074" lon="-0.1278"><time>2025-06-15T12:00:00Z</time></trkpt>
                <trkpt lat="51.5084" lon="-0.1288"><time>2025-06-15T12:05:00Z</time></trkpt>
              </trkseg></trk>
            </gpx>
            """;

        var points = _parser.ParseXml(noNsGpx);

        Assert.Equal(2, points.Count);
        Assert.Equal(51.5074, points[0].Coordinate.Latitude);
        Assert.Equal(-0.1278, points[0].Coordinate.Longitude);
    }

    [Fact]
    public void WhenParsingEmptyTrackThenReturnsEmpty()
    {
        const string emptyTrackGpx = """
            <?xml version="1.0" encoding="UTF-8"?>
            <gpx xmlns="http://www.topografix.com/GPX/1/1" version="1.1">
              <trk><trkseg></trkseg></trk>
            </gpx>
            """;

        var points = _parser.ParseXml(emptyTrackGpx);

        Assert.Empty(points);
    }
}
