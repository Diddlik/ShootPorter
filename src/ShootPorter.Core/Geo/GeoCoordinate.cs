namespace ShootPorter.Core.Geo;

/// <summary>
/// Represents a geographic coordinate with latitude, longitude, and optional altitude.
/// </summary>
public sealed record GeoCoordinate(double Latitude, double Longitude, double? Altitude = null);
