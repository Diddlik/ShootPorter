namespace ShootPorter.Core.Profiles;

/// <summary>
/// Identifies a camera and looks up its {T8}/{T9} token values in the user's camera mappings.
/// </summary>
public static class CameraMappingResolver
{
    /// <summary>
    /// Returns "Model (Serial)" when serial identification is on and a serial is known, otherwise the model.
    /// </summary>
    public static string? GetCameraIdentifier(string? cameraModel, string? serialNumber, bool identifyBySerial) =>
        identifyBySerial && !string.IsNullOrWhiteSpace(serialNumber)
            ? $"{cameraModel} ({serialNumber})"
            : cameraModel;

    public static IReadOnlyDictionary<string, string> Resolve(
        IEnumerable<SavedCameraMapping> mappings, string? cameraModel, string? serialNumber, bool identifyBySerial)
    {
        ArgumentNullException.ThrowIfNull(mappings);

        var identifier = GetCameraIdentifier(cameraModel, serialNumber, identifyBySerial);
        if (string.IsNullOrWhiteSpace(identifier))
            return new Dictionary<string, string>();

        var match = mappings.FirstOrDefault(m => m.CameraModel.Equals(identifier, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { ["T8"] = match.T8Value, ["T9"] = match.T9Value };
    }
}
