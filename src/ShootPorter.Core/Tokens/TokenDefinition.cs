namespace ShootPorter.Core.Tokens;

/// <summary>
/// Describes a single named token, its human-readable description, and which category it belongs to.
/// </summary>
public sealed record TokenDefinition(string Name, string Description, TokenCategory Category);
