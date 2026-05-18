namespace ShootPorter.Core.Dedup;

/// <summary>
/// Checks a collection of source files against a destination directory to determine which need downloading.
/// </summary>
public sealed class DuplicateChecker
{
    private readonly FileComparer _comparer = new();

    /// <summary>
    /// Evaluates each source file against its expected destination path.
    /// </summary>
    public IReadOnlyList<FileComparisonResult> CheckFiles(
        IEnumerable<string> sourcePaths,
        Func<string, string> destinationPathResolver)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        ArgumentNullException.ThrowIfNull(destinationPathResolver);

        var results = new List<FileComparisonResult>();

        foreach (var sourcePath in sourcePaths)
        {
            var destPath = destinationPathResolver(sourcePath);
            var result = _comparer.Compare(sourcePath, destPath);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Returns only the files that need to be downloaded (status is New).
    /// </summary>
    public IReadOnlyList<FileComparisonResult> GetNewFiles(
        IEnumerable<string> sourcePaths,
        Func<string, string> destinationPathResolver)
    {
        return CheckFiles(sourcePaths, destinationPathResolver)
            .Where(r => r.Status == FileStatus.New)
            .ToList();
    }
}
