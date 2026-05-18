using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Display item representing a discovered file in the source browser grid.
/// </summary>
public partial class SourceFileItem : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _extension = string.Empty;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private string _fileType = string.Empty;

    [ObservableProperty]
    private string _status = "New file";

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private DateTime? _fileDate;

    [ObservableProperty]
    private string _downloadPath = string.Empty;

    public string SizeDisplay => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):F1} MB",
    };
    
    public string DateDisplay => FileDate?.ToString("MM/dd/yy HH:mm:ss") ?? string.Empty;

    public string SourceFolderName => Path.GetDirectoryName(FullPath) is { } dir
        ? Path.GetFileName(dir)
        : string.Empty;
}
