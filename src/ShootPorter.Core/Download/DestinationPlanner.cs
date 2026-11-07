using ShootPorter.Core.Tokens;

namespace ShootPorter.Core.Download;

/// <summary>
/// A file's planned destination and whether another file in the same batch resolves to the same path.
/// </summary>
public sealed record PlannedDestination(string Path, bool IsClash);

/// <summary>
/// Resolves destination paths for a batch from a naming template and flags paths that more than one file would use.
/// </summary>
public static class DestinationPlanner
{
    public static IReadOnlyList<PlannedDestination> Plan(
        TokenParser parser, string template, string destinationRoot, IEnumerable<TokenContext> contexts)
    {
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(contexts);

        var paths = contexts
            .Select(c => Path.Combine(destinationRoot, parser.Parse(template, c) + c.Extension))
            .ToList();

        var counts = paths
            .GroupBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        return paths.Select(p => new PlannedDestination(p, counts[p] > 1)).ToList();
    }
}
