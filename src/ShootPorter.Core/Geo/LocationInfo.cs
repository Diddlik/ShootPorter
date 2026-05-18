namespace ShootPorter.Core.Geo;

/// <summary>
/// Reverse-geocoded location information for a geographic coordinate.
/// </summary>
public sealed record LocationInfo(
    string? City,
    string? State,
    string? Country,
    string? CountryCode);
