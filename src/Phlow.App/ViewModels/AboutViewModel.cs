using System.Collections.Generic;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Phlow.App.ViewModels;

/// <summary>
/// ViewModel for the About dialog showing application info and credits.
/// </summary>
public partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isCreditsExpanded;

    public string AppVersion { get; }

    public string DotNetVersion { get; } = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    public IReadOnlyList<string> ReleaseHighlights { get; } =
    [
        "Simplified source picker with recent folders and persistent source history.",
        "Readable transfer grid headers with status filter chips.",
        "Aligned Avalonia and SukiUI package versions for stable startup.",
        "Improved profile, camera mapping, and custom action settings flow.",
    ];

    public IReadOnlyList<CreditEntry> Credits { get; } =
    [
        new("Avalonia UI", "11.3.14", "Cross-platform .NET UI framework", "https://avaloniaui.net"),
        new("SukiUI", "6.1.0", "Modern Avalonia theme and controls", "https://github.com/kikipoulet/SukiUI"),
        new("CommunityToolkit.Mvvm", "8.4.2", "MVVM source generators and helpers", "https://github.com/CommunityToolkit/dotnet"),
        new("MetadataExtractor", "2.9.3", "EXIF, IPTC, and XMP metadata reader", "https://github.com/drewnoakes/metadata-extractor-dotnet"),
        new("Velopack", "0.0.1298", "Auto-update and installer framework", "https://velopack.io"),
    ];

    public AboutViewModel()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        // Strip build metadata suffix (e.g. "0.1.1+sha.abc123" -> "0.1.1")
        if (infoVersion is not null)
        {
            var plusIndex = infoVersion.IndexOf('+');
            AppVersion = plusIndex >= 0 ? infoVersion[..plusIndex] : infoVersion;
        }
        else
        {
            var version = assembly.GetName().Version;
            AppVersion = version is not null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.1";
        }
    }
}

/// <summary>
/// Represents a third-party package credit entry.
/// </summary>
public record CreditEntry(string Name, string Version, string Description, string Url);
