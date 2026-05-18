using System.Text.Json.Serialization;

namespace ShootPorter.Core.Profiles;

/// <summary>
/// A named collection of settings that defines a complete download workflow.
/// </summary>
public sealed record UserProfile
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("destinationRoot")]
    public string DestinationRoot { get; init; } = string.Empty;

    [JsonPropertyName("destinationTemplate")]
    public string DestinationTemplate { get; init; } = @"{Y}-{m}-{D}\{OriginalFilename}{Extension}";

    [JsonPropertyName("backupPaths")]
    public IReadOnlyList<string> BackupPaths { get; init; } = [];

    [JsonPropertyName("maxParallelism")]
    public int MaxParallelism { get; init; } = 2;

    [JsonPropertyName("verifyAfterCopy")]
    public bool VerifyAfterCopy { get; init; } = true;

    [JsonPropertyName("autoDeleteSource")]
    public bool AutoDeleteSource { get; init; }

    [JsonPropertyName("defaultJobCode")]
    public string DefaultJobCode { get; init; } = string.Empty;
}
