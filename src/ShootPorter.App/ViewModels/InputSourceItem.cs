using CommunityToolkit.Mvvm.ComponentModel;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Represents a selectable input source such as a removable drive or custom folder.
/// </summary>
public partial class InputSourceItem : ObservableObject
{
    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _icon = "💾";

    [ObservableProperty]
    private bool _isRemovable;

    [ObservableProperty]
    private long _totalSizeBytes;

    [ObservableProperty]
    private long _availableSizeBytes;

    public string SizeDisplay => TotalSizeBytes switch
    {
        0 => string.Empty,
        < 1024L * 1024 * 1024 => $"{TotalSizeBytes / (1024.0 * 1024.0):F0} MB",
        _ => $"{TotalSizeBytes / (1024.0 * 1024.0 * 1024.0):F1} GB",
    };
}
