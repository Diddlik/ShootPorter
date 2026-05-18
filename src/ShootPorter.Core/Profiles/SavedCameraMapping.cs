using System.Text.Json.Serialization;

namespace ShootPorter.Core.Profiles;

/// <summary>
/// Serializable representation of a camera mapping entry for settings persistence.
/// </summary>
public sealed record SavedCameraMapping
{
    [JsonPropertyName("cameraModel")]
    public string CameraModel { get; init; } = string.Empty;

    [JsonPropertyName("t8Value")]
    public string T8Value { get; init; } = string.Empty;

    [JsonPropertyName("t9Value")]
    public string T9Value { get; init; } = string.Empty;
}
