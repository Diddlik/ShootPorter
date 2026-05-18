namespace ShootPorter.Core.Cli;

/// <summary>
/// Parsed command-line options for headless/batch operation.
/// </summary>
public sealed record CliOptions
{
    public string? SourcePath { get; init; }
    public string? DestinationRoot { get; init; }
    public string? ProfileName { get; init; }
    public string? JobCode { get; init; }
    public bool Recursive { get; init; } = true;
    public int MaxParallelism { get; init; } = 2;
    public bool VerifyAfterCopy { get; init; } = true;
    public bool AutoDeleteSource { get; init; }
    public bool ShowHelp { get; init; }
    public bool Headless { get; init; }
}
