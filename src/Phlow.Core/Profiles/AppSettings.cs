using System.Text.Json.Serialization;

namespace Phlow.Core.Profiles;

/// <summary>
/// Top-level settings model persisted to the roaming AppData directory.
/// </summary>
public sealed record AppSettings
{
    // General
    [JsonPropertyName("checkForUpdates")]
    public bool CheckForUpdates { get; init; } = true;

    [JsonPropertyName("isAutoDetect")]
    public bool IsAutoDetect { get; init; }

    [JsonPropertyName("isCameraUsb")]
    public bool IsCameraUsb { get; init; }

    [JsonPropertyName("isCardReader")]
    public bool IsCardReader { get; init; } = true;

    [JsonPropertyName("driveLetter")]
    public string DriveLetter { get; init; } = "J";

    [JsonPropertyName("disablePtpDownloads")]
    public bool DisablePtpDownloads { get; init; } = true;

    [JsonPropertyName("useCaptureTime")]
    public bool UseCaptureTime { get; init; } = true;

    [JsonPropertyName("autoEjectMedia")]
    public bool AutoEjectMedia { get; init; } = true;

    [JsonPropertyName("saveReadOnly")]
    public bool SaveReadOnly { get; init; }

    [JsonPropertyName("autoRotateJpegs")]
    public bool AutoRotateJpegs { get; init; }

    [JsonPropertyName("renameJpeToJpg")]
    public bool RenameJpeToJpg { get; init; }

    [JsonPropertyName("convertToLowerCase")]
    public bool ConvertToLowerCase { get; init; }

    [JsonPropertyName("addIptcXmpData")]
    public bool AddIptcXmpData { get; init; }

    // Camera
    [JsonPropertyName("identifyCameraBySerial")]
    public bool IdentifyCameraBySerial { get; init; } = true;

    // Custom Actions
    [JsonPropertyName("enableCustomButton")]
    public bool EnableCustomButton { get; init; } = true;

    [JsonPropertyName("customButtonCaption")]
    public string CustomButtonCaption { get; init; } = "Exif+Move";

    [JsonPropertyName("customScriptPath")]
    public string CustomScriptPath { get; init; } = string.Empty;

    // Download
    [JsonPropertyName("backupPath1")]
    public string BackupPath1 { get; init; } = string.Empty;

    [JsonPropertyName("backupPath2")]
    public string BackupPath2 { get; init; } = string.Empty;

    [JsonPropertyName("maxParallelism")]
    public int MaxParallelism { get; init; } = 2;

    [JsonPropertyName("verifyAfterCopy")]
    public bool VerifyAfterCopy { get; init; } = true;

    [JsonPropertyName("autoDeleteSource")]
    public bool AutoDeleteSource { get; init; }

    [JsonPropertyName("selectedSourcePath")]
    public string SelectedSourcePath { get; init; } = string.Empty;

    [JsonPropertyName("recentSourcePaths")]
    public IReadOnlyList<string> RecentSourcePaths { get; init; } = [];

    // Profiles
    [JsonPropertyName("profiles")]
    public IReadOnlyList<SavedProfile> Profiles { get; init; } = [];

    [JsonPropertyName("selectedProfileName")]
    public string SelectedProfileName { get; init; } = string.Empty;

    // Camera Mappings
    [JsonPropertyName("cameraMappings")]
    public IReadOnlyList<SavedCameraMapping> CameraMappings { get; init; } = [];
}
