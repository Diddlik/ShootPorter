namespace ShootPorter.Core.Geo;

/// <summary>
/// Abstracts reverse geocoding to allow testing without network access.
/// </summary>
public interface IGeocodingService
{
    Task<LocationInfo?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
