using System.ComponentModel;
using System.Diagnostics;
using Serilog;

namespace ShootPorter.Core.Plugins;

/// <summary>
/// Runs the user's custom action script for a downloaded file, passing the file path and its folder.
/// </summary>
public static class CustomScriptRunner
{
    /// <summary>
    /// Output is not redirected: an unread pipe blocks the script once its buffer fills.
    /// PowerShell scripts cannot be started directly without the shell, so they run through PowerShell.
    /// </summary>
    public static ProcessStartInfo CreateStartInfo(string scriptPath, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var startInfo = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true };
        if (string.Equals(Path.GetExtension(scriptPath), ".ps1", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh";
            foreach (var arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath })
                startInfo.ArgumentList.Add(arg);
        }
        else
        {
            startInfo.FileName = scriptPath;
        }

        startInfo.ArgumentList.Add(filePath);
        startInfo.ArgumentList.Add(Path.GetDirectoryName(filePath) ?? string.Empty);
        return startInfo;
    }

    /// <summary>
    /// Runs the script and waits for it to exit. Returns false when it could not be started.
    /// </summary>
    public static async Task<bool> RunAsync(string scriptPath, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = Process.Start(CreateStartInfo(scriptPath, filePath));
            if (process is null)
                return false;

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Win32Exception ex)
        {
            Log.Warning(ex, "Custom script {ScriptPath} could not be started for {FilePath}", scriptPath, filePath);
            return false;
        }
    }
}
