using System.Diagnostics;

namespace ShootPorter.Core.Plugins;

/// <summary>
/// Converts RAW files to DNG format using Adobe DNG Converter.
/// Requires Adobe DNG Converter to be installed on the system.
/// </summary>
public sealed class DngConverterPlugin : IPostDownloadPlugin
{
    private readonly string _dngConverterPath;

    public DngConverterPlugin(string? dngConverterPath = null)
    {
        _dngConverterPath = dngConverterPath
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Adobe", "Adobe DNG Converter", "Adobe DNG Converter.exe");
    }

    public string Name => "DNG Converter";
    public int Order => 20;
    public bool IsEnabled { get; set; }

    private static readonly HashSet<string> RawExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf", ".pef", ".gpr"
    };

    public async Task<bool> ProcessAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var ext = Path.GetExtension(filePath);
        if (!RawExtensions.Contains(ext))
            return true; // not a RAW file, skip

        if (!File.Exists(_dngConverterPath))
            return false; // converter not installed

        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _dngConverterPath,
                Arguments = $"-c \"{filePath}\" \"{directory}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode == 0;
    }
}
