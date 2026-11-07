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
        _dngConverterPath = !string.IsNullOrWhiteSpace(dngConverterPath)
            ? dngConverterPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Adobe", "Adobe DNG Converter", "Adobe DNG Converter.exe");
    }

    public string Name => "DNG Converter";
    public int Order => 20;
    public bool IsEnabled { get; set; }

    private static readonly HashSet<string> RawExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cr2", ".cr3", ".nef", ".nrw", ".arw", ".raf", ".orf", ".pef", ".gpr", ".rw2", ".srw", ".3fr", ".iiq"
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

        // Output is not redirected: an unread pipe can fill up and block the converter.
        var startInfo = new ProcessStartInfo(_dngConverterPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("-d");
        startInfo.ArgumentList.Add(directory);
        startInfo.ArgumentList.Add(filePath);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {_dngConverterPath}");
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode == 0;
    }
}
