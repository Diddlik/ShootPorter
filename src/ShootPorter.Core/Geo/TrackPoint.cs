namespace ShootPorter.Core.Geo;

/// <summary>
/// A single timestamped point from a GPS track log.
/// </summary>
public sealed record TrackPoint(DateTimeOffset Timestamp, GeoCoordinate Coordinate);
