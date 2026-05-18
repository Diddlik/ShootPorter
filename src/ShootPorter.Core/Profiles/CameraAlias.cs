using System.Text.Json.Serialization;

namespace ShootPorter.Core.Profiles;

/// <summary>
/// Maps a camera identifier (serial number or model string) to a user-friendly name.
/// </summary>
public sealed record CameraAlias
{
    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    [JsonPropertyName("friendlyName")]
    public required string FriendlyName { get; init; }
}
