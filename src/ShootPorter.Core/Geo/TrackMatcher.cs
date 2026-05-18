namespace ShootPorter.Core.Geo;

/// <summary>
/// Matches photo capture timestamps to GPS track points, interpolating position between adjacent points.
/// </summary>
public sealed class TrackMatcher
{
    private readonly TimeSpan _maxTimeDrift;

    /// <param name="maxTimeDrift">Maximum time difference allowed between a photo and the nearest track point. Default 5 minutes.</param>
    public TrackMatcher(TimeSpan? maxTimeDrift = null)
    {
        _maxTimeDrift = maxTimeDrift ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Finds the interpolated coordinate for a given timestamp within the track.
    /// Returns null if no track point is within the max drift window.
    /// </summary>
    public GeoCoordinate? Match(DateTimeOffset photoTimestamp, IReadOnlyList<TrackPoint> sortedTrack)
    {
        ArgumentNullException.ThrowIfNull(sortedTrack);

        if (sortedTrack.Count == 0)
            return null;

        // Binary search for the insertion point
        var index = BinarySearchClosest(sortedTrack, photoTimestamp);

        // Check if exact match or within bounds
        if (index >= 0 && index < sortedTrack.Count)
        {
            var point = sortedTrack[index];
            var diff = (photoTimestamp - point.Timestamp).Duration();

            if (diff <= _maxTimeDrift)
            {
                // Try to interpolate between adjacent points
                if (index > 0 && index < sortedTrack.Count)
                {
                    var before = sortedTrack[index - 1];
                    var after = sortedTrack[index];

                    if (photoTimestamp >= before.Timestamp && photoTimestamp <= after.Timestamp)
                        return Interpolate(before, after, photoTimestamp);
                }

                return point.Coordinate;
            }
        }

        // Check neighbors
        if (index > 0)
        {
            var prev = sortedTrack[index - 1];
            if ((photoTimestamp - prev.Timestamp).Duration() <= _maxTimeDrift)
                return prev.Coordinate;
        }

        if (index < sortedTrack.Count)
        {
            var next = sortedTrack[index];
            if ((photoTimestamp - next.Timestamp).Duration() <= _maxTimeDrift)
                return next.Coordinate;
        }

        return null;
    }

    private static int BinarySearchClosest(IReadOnlyList<TrackPoint> track, DateTimeOffset target)
    {
        int lo = 0, hi = track.Count - 1;

        while (lo <= hi)
        {
            var mid = lo + (hi - lo) / 2;
            var cmp = track[mid].Timestamp.CompareTo(target);

            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        return lo; // insertion point
    }

    private static GeoCoordinate Interpolate(TrackPoint before, TrackPoint after, DateTimeOffset target)
    {
        var totalSpan = (after.Timestamp - before.Timestamp).TotalSeconds;
        if (totalSpan <= 0)
            return before.Coordinate;

        var fraction = (target - before.Timestamp).TotalSeconds / totalSpan;

        var lat = before.Coordinate.Latitude + (after.Coordinate.Latitude - before.Coordinate.Latitude) * fraction;
        var lon = before.Coordinate.Longitude + (after.Coordinate.Longitude - before.Coordinate.Longitude) * fraction;

        double? alt = null;
        if (before.Coordinate.Altitude.HasValue && after.Coordinate.Altitude.HasValue)
            alt = before.Coordinate.Altitude.Value + (after.Coordinate.Altitude.Value - before.Coordinate.Altitude.Value) * fraction;

        return new GeoCoordinate(lat, lon, alt);
    }
}
