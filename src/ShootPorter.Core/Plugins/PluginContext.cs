namespace ShootPorter.Core.Plugins;

/// <summary>
/// Provides context information to plugins during execution.
/// </summary>
public sealed record PluginContext(
    string SourcePath,
    string DestinationPath,
    string DestinationDirectory);
