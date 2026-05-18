namespace ShootPorter.Core.Cli;

/// <summary>
/// Parses command-line arguments into a CliOptions record.
/// Supports --source, --dest, --profile, --jobcode, --parallel, --no-verify, --auto-delete, --headless, --help.
/// </summary>
public sealed class CliParser
{
    /// <summary>
    /// Parses command-line arguments. Unknown arguments are ignored.
    /// </summary>
    public CliOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var source = (string?)null;
        var dest = (string?)null;
        var profile = (string?)null;
        var jobCode = (string?)null;
        var recursive = true;
        var parallel = 2;
        var verify = true;
        var autoDelete = false;
        var help = false;
        var headless = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg.ToLowerInvariant())
            {
                case "--source" or "-s" when i + 1 < args.Length:
                    source = args[++i];
                    break;
                case "--dest" or "-d" when i + 1 < args.Length:
                    dest = args[++i];
                    break;
                case "--profile" or "-p" when i + 1 < args.Length:
                    profile = args[++i];
                    break;
                case "--jobcode" or "-j" when i + 1 < args.Length:
                    jobCode = args[++i];
                    break;
                case "--parallel" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var p)) parallel = p;
                    break;
                case "--no-recursive":
                    recursive = false;
                    break;
                case "--no-verify":
                    verify = false;
                    break;
                case "--auto-delete":
                    autoDelete = true;
                    break;
                case "--headless":
                    headless = true;
                    break;
                case "--help" or "-h":
                    help = true;
                    break;
            }
        }

        return new CliOptions
        {
            SourcePath = source,
            DestinationRoot = dest,
            ProfileName = profile,
            JobCode = jobCode,
            Recursive = recursive,
            MaxParallelism = parallel,
            VerifyAfterCopy = verify,
            AutoDeleteSource = autoDelete,
            ShowHelp = help,
            Headless = headless,
        };
    }

    /// <summary>
    /// Returns a help text string describing available CLI options.
    /// </summary>
    public static string GetHelpText() => """
        ShootPorter V2 - Photo/Video Download Organizer

        Usage: ShootPorter [options]

        Options:
          --source, -s <path>     Source folder or drive to scan
          --dest, -d <path>       Destination root folder
          --profile, -p <name>    Load a named settings profile
          --jobcode, -j <code>    Set the job code for this session
          --parallel <n>          Max parallel file copies (default: 2)
          --no-recursive          Don't scan subdirectories
          --no-verify             Skip post-copy verification
          --auto-delete           Delete source files after verified copy
          --headless              Run without UI (batch mode)
          --help, -h              Show this help text
        """;
}
