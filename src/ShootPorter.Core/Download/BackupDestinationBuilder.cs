namespace ShootPorter.Core.Download;

/// <summary>
/// Normalizes configured backup destination roots before a download starts.
/// </summary>
public static class BackupDestinationBuilder
{
    public static IReadOnlyList<string> FromPaths(params string?[] paths)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var normalized = Path.GetFullPath(path.Trim());
            var trimmed = normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (trimmed.Length == 0)
                continue;

            if (seen.Add(trimmed))
                result.Add(trimmed);
        }

        return result;
    }
}
