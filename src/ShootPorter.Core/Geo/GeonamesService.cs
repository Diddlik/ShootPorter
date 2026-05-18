using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ShootPorter.Core.Geo;

/// <summary>
/// Reverse geocodes coordinates using the Geonames.org API with in-memory caching.
/// </summary>
public sealed class GeonamesService : IGeocodingService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _username;
    private readonly Dictionary<(int LatKey, int LonKey), LocationInfo> _cache = [];

    /// <param name="username">Geonames API username (free registration at geonames.org).</param>
    /// <param name="httpClient">Optional HttpClient for testing. If null, creates a new one.</param>
    public GeonamesService(string username, HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        _username = username;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<LocationInfo?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        // Cache key rounded to ~1km precision (2 decimal places)
        var cacheKey = ((int)(latitude * 100), (int)(longitude * 100));
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        var url = string.Format(
            CultureInfo.InvariantCulture,
            "http://api.geonames.org/findNearbyPlaceNameJSON?lat={0}&lng={1}&username={2}",
            latitude, longitude, _username);

        try
        {
            var response = await _httpClient.GetFromJsonAsync<GeonamesResponse>(url, cancellationToken).ConfigureAwait(false);
            var place = response?.Geonames?.FirstOrDefault();

            if (place is null)
                return null;

            var info = new LocationInfo(
                City: place.Name,
                State: place.AdminName1,
                Country: place.CountryName,
                CountryCode: place.CountryCode);

            _cache[cacheKey] = info;
            return info;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    // JSON response DTOs
    private sealed class GeonamesResponse
    {
        [JsonPropertyName("geonames")]
        public List<GeonamesPlace>? Geonames { get; set; }
    }

    private sealed class GeonamesPlace
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("adminName1")]
        public string? AdminName1 { get; set; }

        [JsonPropertyName("countryName")]
        public string? CountryName { get; set; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }
    }
}
