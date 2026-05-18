using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Velopack;

namespace ShootPorter.App;

/// <summary>
/// Application entry point. Velopack hooks run before Avalonia initializes.
/// </summary>
sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack must be first — it may exit the process for update hooks
        var builder = VelopackApp.Build();
        if (OperatingSystem.IsWindows())
            RegisterWindowsHooks(builder);
        builder.Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void RegisterWindowsHooks(VelopackApp builder)
    {
        builder
            .OnAfterInstallFastCallback(v => ManageShortcuts(install: true))
            .OnAfterUpdateFastCallback(v => ManageShortcuts(install: true))
            .OnBeforeUninstallFastCallback(v => ManageShortcuts(install: false));
    }

    private static void ManageShortcuts(bool install)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var desktopLink = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ShootPorter.lnk");
        var startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
        var startMenuLink = Path.Combine(startMenuDir, "ShootPorter.lnk");

        if (install)
        {
            var exePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ShootPorter", "current", "ShootPorter.exe");

            CreateWindowsShortcut(desktopLink, exePath);
            Directory.CreateDirectory(startMenuDir);
            CreateWindowsShortcut(startMenuLink, exePath);
        }
        else
        {
            if (File.Exists(desktopLink)) File.Delete(desktopLink);
            if (File.Exists(startMenuLink)) File.Delete(startMenuLink);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void CreateWindowsShortcut(string linkPath, string targetPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) return;

        dynamic? shell = null;
        try
        {
            shell = Activator.CreateInstance(shellType);
            if (shell == null) return;

            var shortcut = shell.CreateShortcut(linkPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.Description = "ShootPorter — Photo & Video Downloader";
            shortcut.Save();
        }
        finally
        {
            if (shell != null)
                Marshal.ReleaseComObject(shell);
        }
    }
}
