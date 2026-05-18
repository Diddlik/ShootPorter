using System.Text.Json.Serialization;

namespace ShootPorter.Core.Profiles;

/// <summary>
/// Serializable representation of a user profile for settings persistence.
/// </summary>
public sealed record SavedProfile
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("namingTemplate")]
    public string NamingTemplate { get; init; } = string.Empty;

    [JsonPropertyName("canDelete")]
    public bool CanDelete { get; init; } = true;
}
