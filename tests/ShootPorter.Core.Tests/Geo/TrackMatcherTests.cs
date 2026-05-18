using ShootPorter.Core.Geo;

namespace ShootPorter.Core.Tests.Geo;

/// <summary>
/// Tests for <see cref="TrackMatcher"/> covering interpolation, drift window, and edge cases.
/// </summary>
public sealed class TrackMatcherTests
{
    private static readonly DateTimeOffset BaseTime = new(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private static IReadOnlyList<TrackPoint> BuildTrack() =>
    [
        new TrackPoint(BaseTime,                    new GeoCoordinate(47.6062, -122.3321, 56.0)),
        new TrackPoint(BaseTime.AddMinutes(5),      new GeoCoordinate(47.6072, -122.3331, 58.0)),
        new TrackPoint(BaseTime.AddMinutes(10),     new GeoCoordinate(47.6082, -122.3341, 60.0)),
    ];

    [Fact]
    public void WhenPhotoTimestampMatchesExactPointThenReturnsCoordinate()
    {
        var matcher = new TrackMatcher();
        var track = BuildTrack();

        var result = matcher.Match(BaseTime, track);

        Assert.NotNull(result);
        Assert.Equal(47.6062, result.Latitude);
        Assert.Equal(-122.3321, result.Longitude);
    }

    [Fact]
    public void WhenPhotoTimestampBetweenPointsThenInterpolates()
    {
        var matcher = new TrackMatcher();
        var track = BuildTrack();

        // Midpoint between first (10:00) and second (10:05) track points
        var midTime = BaseTime.AddMinutes(2.5);
        var result = matcher.Match(midTime, track);

        Assert.NotNull(result);
        // At 50% between the two points
        Assert.Equal(47.6067, result.Latitude, precision: 4);
        Assert.Equal(-122.3326, result.Longitude, precision: 4);
        Assert.Equal(57.0, result.Altitude);
    }

    [Fact]
    public void WhenPhotoTimestampOutsideDriftWindowThenReturnsNull()
    {
        var matcher = new TrackMatcher(TimeSpan.FromMinutes(5));
        var track = BuildTrack();

        // 6 minutes before first point — beyond default 5-min drift
        var tooEarly = BaseTime.AddMinutes(-6);
        var result = matcher.Match(tooEarly, track);

        Assert.Null(result);
    }

    [Fact]
    public void WhenPhotoTimestampWithinDriftWindowThenReturnsCoordinate()
    {
        var matcher = new TrackMatcher(TimeSpan.FromMinutes(5));
        var track = BuildTrack();

        // 4 minutes before first point — within drift window
        var nearStart = BaseTime.AddMinutes(-4);
        var result = matcher.Match(nearStart, track);

        Assert.NotNull(result);
    }

    [Fact]
    public void WhenTrackIsEmptyThenReturnsNull()
    {
        var matcher = new TrackMatcher();

        var result = matcher.Match(BaseTime, []);

        Assert.Null(result);
    }

    [Fact]
    public void WhenCustomDriftWindowThenRespectsThatWindow()
    {
        // Tight 1-minute drift
        var matcher = new TrackMatcher(TimeSpan.FromMinutes(1));
        var track = BuildTrack();

        // 2 minutes after last point (10:10) is outside 1-min window
        var tooLate = BaseTime.AddMinutes(12);
        var result = matcher.Match(tooLate, track);

        Assert.Null(result);
    }
}
