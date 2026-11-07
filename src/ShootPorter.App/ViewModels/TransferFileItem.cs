using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using Avalonia.Media;
using ShootPorter.Core.Download;
using ShootPorter.Core.Metadata;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Transfer state of a file in the queue.
/// </summary>
public enum FileTransferStatus
{
    Pending,
    Downloading,
    Completed,
    NameClash,
    Error,
    AlreadyDownloaded
}

/// <summary>
/// A file in the transfer queue with its destination preview and transfer state.
/// </summary>
public partial class TransferFileItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private FileTransferStatus _status;

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private DateTime _captureDate;

    [ObservableProperty]
    private string _destinationPreview = string.Empty;

    [ObservableProperty]
    private string _sourcePath = string.Empty;

    [ObservableProperty]
    private SourceFileDisposition _sourceDisposition;

    [ObservableProperty]
    private string _statusDetail = string.Empty;

    public FileMetadata? Metadata { get; init; }

    public string SizeDisplay => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):F1} MB",
    };

    public string CaptureDateDisplay => CaptureDate.ToString("dd/MM/yy");
    public string CaptureTimeDisplay => CaptureDate.ToString("HH:mm:ss");

    public string SourceFolderName => Path.GetDirectoryName(SourcePath) is { } dir
        ? Path.GetFileName(dir)
        : string.Empty;

    public string StatusText => Status switch
    {
        FileTransferStatus.Error when SourceDisposition == SourceFileDisposition.DeleteFailed => "Delete failed",
        FileTransferStatus.Completed when SourceDisposition == SourceFileDisposition.Deleted => "Deleted",
        FileTransferStatus.Completed when SourceDisposition == SourceFileDisposition.Retained => "Retained",
        FileTransferStatus.Completed => "Completed",
        FileTransferStatus.AlreadyDownloaded => "Already downloaded",
        FileTransferStatus.Downloading => "Downloading",
        FileTransferStatus.NameClash => "Name clash",
        FileTransferStatus.Error => "Error",
        _ => "Pending"
    };

    public string StatusIcon => Status switch
    {
        FileTransferStatus.Completed or FileTransferStatus.AlreadyDownloaded => "Checkmark",
        FileTransferStatus.Downloading => "Sync",
        FileTransferStatus.NameClash => "Warning",
        FileTransferStatus.Error => "Dismiss",
        _ => "Info"
    };

    public IBrush StatusBackground => Status switch
    {
        FileTransferStatus.Completed or FileTransferStatus.AlreadyDownloaded => BrushCache.Get("#dcfce7"),
        FileTransferStatus.Downloading => BrushCache.Get("#dbeafe"),
        FileTransferStatus.NameClash => BrushCache.Get("#fef3c7"),
        FileTransferStatus.Error => BrushCache.Get("#fee2e2"),
        _ => BrushCache.Get("#f1f5f9")
    };

    public IBrush StatusBorder => Status switch
    {
        FileTransferStatus.Completed or FileTransferStatus.AlreadyDownloaded => BrushCache.Get("#86efac"),
        FileTransferStatus.Downloading => BrushCache.Get("#93c5fd"),
        FileTransferStatus.NameClash => BrushCache.Get("#fcd34d"),
        FileTransferStatus.Error => BrushCache.Get("#fca5a5"),
        _ => BrushCache.Get("#e2e8f0")
    };

    public IBrush StatusForeground => Status switch
    {
        FileTransferStatus.Completed or FileTransferStatus.AlreadyDownloaded => BrushCache.Get("#166534"),
        FileTransferStatus.Downloading => BrushCache.Get("#1d4ed8"),
        FileTransferStatus.NameClash => BrushCache.Get("#92400e"),
        FileTransferStatus.Error => BrushCache.Get("#991b1b"),
        _ => BrushCache.Get("#475569")
    };

    public IBrush ProgressColor => Status switch
    {
        FileTransferStatus.Completed or FileTransferStatus.AlreadyDownloaded => BrushCache.Get("#10b981"),
        FileTransferStatus.Downloading => BrushCache.Get("#3b82f6"),
        FileTransferStatus.NameClash => BrushCache.Get("#f59e0b"),
        _ => BrushCache.Get("#cbd5e1")
    };

    partial void OnStatusChanged(FileTransferStatus value)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusIcon));
        OnPropertyChanged(nameof(StatusBackground));
        OnPropertyChanged(nameof(StatusBorder));
        OnPropertyChanged(nameof(StatusForeground));
        OnPropertyChanged(nameof(ProgressColor));
    }

    partial void OnSourceDispositionChanged(SourceFileDisposition value)
    {
        OnPropertyChanged(nameof(StatusText));
    }
}
