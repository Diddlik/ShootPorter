using ShootPorter.App.Properties;
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
        FileTransferStatus.Error when SourceDisposition == SourceFileDisposition.DeleteFailed => Resources.StatusDeleteFailed,
        FileTransferStatus.Completed when SourceDisposition == SourceFileDisposition.Deleted => Resources.StatusDeleted,
        FileTransferStatus.Completed when SourceDisposition == SourceFileDisposition.Retained => Resources.StatusRetained,
        FileTransferStatus.Completed => Resources.StatusCompleted,
        FileTransferStatus.AlreadyDownloaded => Resources.StatusAlreadyDownloaded,
        FileTransferStatus.Downloading => Resources.StatusDownloading,
        FileTransferStatus.NameClash => Resources.StatusNameClash,
        FileTransferStatus.Error => Resources.StatusError,
        _ => Resources.StatusPending
    };

    public string StatusIcon => Status switch
    {
        FileTransferStatus.Completed or FileTransferStatus.AlreadyDownloaded => "Checkmark",
        FileTransferStatus.Downloading => "Sync",
        FileTransferStatus.NameClash => "Warning",
        FileTransferStatus.Error => "Dismiss",
        _ => "Info"
    };

    public bool IsDone => Status is FileTransferStatus.Completed or FileTransferStatus.AlreadyDownloaded;
    public bool IsBusy => Status == FileTransferStatus.Downloading;
    public bool IsClash => Status == FileTransferStatus.NameClash;
    public bool IsFailed => Status == FileTransferStatus.Error;

    partial void OnStatusChanged(FileTransferStatus value)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusIcon));
        OnPropertyChanged(nameof(IsDone));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsClash));
        OnPropertyChanged(nameof(IsFailed));
    }

    partial void OnSourceDispositionChanged(SourceFileDisposition value)
    {
        OnPropertyChanged(nameof(StatusText));
    }
}
