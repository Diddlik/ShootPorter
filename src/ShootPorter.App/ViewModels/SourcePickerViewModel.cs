using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ShootPorter.Core.Discovery;
using ShootPorter.Core.Metadata;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Source picker: available drives, folder history, and scanning the selected source into the transfer queue.
/// </summary>
public partial class SourcePickerViewModel : ViewModelBase, IDisposable
{
    private const int MaxRecentSourcePaths = 5;
    private readonly SettingsViewModel _settings;
    private readonly TransferQueueViewModel _queue;
    private readonly DriveWatcherService _driveWatcher = new();
    private CancellationTokenSource? _scanCts;
    private bool _rescanWhenUnlocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SourceStatusText))]
    private bool _isSourceScanning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SourceStatusText))]
    private bool _sourceFolderExists = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SourceStatusText))]
    private int _detectedFileCount;

    [ObservableProperty]
    private InputSourceItem? _selectedInputSource;

    /// <summary>
    /// Set while a download runs; scans requested meanwhile run once it is cleared,
    /// because clearing the list mid-download would hide files that are still being copied.
    /// </summary>
    [ObservableProperty]
    private bool _isLocked;

    public SourcePickerViewModel(SettingsViewModel settings, TransferQueueViewModel queue)
    {
        _settings = settings;
        _queue = queue;
        _settings.PropertyChanged += OnSettingsPropertyChanged;
        _driveWatcher.DriveChanged += OnDriveChanged;
        _driveWatcher.StartWatching();
        RefreshDrives();
    }

    /// <summary>
    /// Raised on the UI thread after a scan has added all files to the queue.
    /// </summary>
    public event EventHandler? ScanCompleted;

    /// <summary>
    /// Raised on the UI thread after a chunk of scanned files was appended to the queue.
    /// </summary>
    public event Action<IReadOnlyList<TransferFileItem>>? FilesAdded;

    public ObservableCollection<InputSourceItem> InputSources { get; } = [];
    public ObservableCollection<string> RecentSourcePaths { get; } = [];

    public bool HasSelectedSourcePath => !string.IsNullOrWhiteSpace(SelectedSourcePath);
    public bool HasRecentSourcePaths => RecentSourcePaths.Count > 0;
    public string SelectedSourcePath => SelectedInputSource?.Path ?? string.Empty;

    public string SourceStatusText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SelectedSourcePath))
                return "Select a folder to detect media files";

            if (!SourceFolderExists)
                return "Folder not found";

            if (IsSourceScanning)
                return "Scanning folder...";

            return DetectedFileCount switch
            {
                0 => "No media files detected",
                1 => "1 file detected",
                _ => $"{DetectedFileCount} files detected"
            };
        }
    }

    public string DisplaySourcePath =>
        string.IsNullOrWhiteSpace(SelectedSourcePath)
            ? "No folder selected"
            : ShortenPath(SelectedSourcePath);

    partial void OnSelectedInputSourceChanged(InputSourceItem? value)
    {
        DetectedFileCount = 0;
        if (value is null)
        {
            SourceFolderExists = true;
            IsSourceScanning = false;
            NotifySourceChanged();
            return;
        }

        SourceFolderExists = Directory.Exists(value.Path);
        PersistSourceHistory(value.Path);
        NotifySourceChanged();
        _ = ScanSelectedSourceAsync();
    }

    partial void OnIsLockedChanged(bool value)
    {
        if (value || !_rescanWhenUnlocked)
            return;

        _rescanWhenUnlocked = false;
        RescanSource();
    }

    [RelayCommand]
    private void RefreshDrives()
    {
        var currentPath = SelectedInputSource?.Path;

        foreach (var old in InputSources.Where(s => s.IsRemovable).ToList())
            InputSources.Remove(old);

        var mode = _settings.IsCameraUsb ? SourceDriveMode.CameraUsb
            : _settings.IsCardReader ? SourceDriveMode.CardReader
            : SourceDriveMode.AutoDetect;

        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                var hasDcim = Directory.Exists(Path.Combine(drive.Name, "DCIM"));
                if (!SourceDriveFilter.IsSource(drive.DriveType, hasDcim, mode))
                    continue;

                var label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Removable Disk" : drive.VolumeLabel;
                InputSources.Add(new InputSourceItem
                {
                    Path = drive.Name,
                    DisplayName = $"{label} ({drive.Name.TrimEnd('\\')})",
                    Icon = hasDcim ? "📷" : "💾",
                    IsRemovable = true,
                    TotalSizeBytes = drive.TotalSize,
                    AvailableSizeBytes = drive.AvailableFreeSpace,
                });
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Drive enumeration failed");
        }

        if (currentPath is not null)
        {
            SelectedInputSource = InputSources.FirstOrDefault(
                                      s => s.Path.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
                                  ?? SelectedInputSource;
        }

        if (SelectedInputSource is null && InputSources.Count > 0)
            SelectedInputSource = InputSources[0];
    }

    public void AddCustomFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return;

        SelectSourcePath(folderPath, addToRecent: true);
    }

    public void SelectRecentSource(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return;

        SelectSourcePath(folderPath, addToRecent: true);
    }

    /// <summary>
    /// Loads the recent folders from settings and selects the last used source.
    /// </summary>
    public void LoadSourceHistory()
    {
        RecentSourcePaths.Clear();
        foreach (var path in _settings.RecentSourcePaths.Where(Directory.Exists).Take(MaxRecentSourcePaths))
            RecentSourcePaths.Add(path);

        OnPropertyChanged(nameof(HasRecentSourcePaths));

        var selectedSourcePath = _settings.SelectedSourcePath;
        if (!string.IsNullOrWhiteSpace(selectedSourcePath))
        {
            SelectSourcePath(selectedSourcePath, addToRecent: false);
            return;
        }

        NotifySourceChanged();
    }

    private void SelectSourcePath(string folderPath, bool addToRecent)
    {
        var existing = InputSources.FirstOrDefault(
            s => s.Path.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
        var sourceWasAlreadySelected = existing is not null && existing == SelectedInputSource;

        if (existing is null)
        {
            existing = new InputSourceItem
            {
                Path = folderPath,
                DisplayName = GetSourceDisplayName(folderPath),
                Icon = "📁",
                IsRemovable = false,
            };
            InputSources.Add(existing);
        }

        SelectedInputSource = existing;

        if (addToRecent)
            AddRecentSourcePath(folderPath);

        if (sourceWasAlreadySelected)
            RescanSource();
    }

    [RelayCommand]
    private void RescanSource()
    {
        if (string.IsNullOrWhiteSpace(SelectedSourcePath) || DeferScanWhileLocked())
            return;

        SourceFolderExists = Directory.Exists(SelectedSourcePath);
        if (!SourceFolderExists)
        {
            DetectedFileCount = 0;
            _queue.ClearFiles();
            NotifySourceChanged();
            return;
        }

        _ = ScanSelectedSourceAsync();
    }

    private bool DeferScanWhileLocked()
    {
        if (!IsLocked)
            return false;

        _rescanWhenUnlocked = true;
        return true;
    }

    private async Task ScanSelectedSourceAsync()
    {
        if (DeferScanWhileLocked())
            return;

        _scanCts?.Cancel();
        using var scanCts = new CancellationTokenSource();
        _scanCts = scanCts;
        try
        {
            await ScanSourceCoreAsync(scanCts.Token);
        }
        finally
        {
            if (_scanCts == scanCts)
                _scanCts = null;
        }
    }

    private async Task ScanSourceCoreAsync(CancellationToken ct)
    {
        var path = SelectedInputSource?.Path;
        if (string.IsNullOrWhiteSpace(path))
            return;

        _queue.ClearFiles();
        IsSourceScanning = true;
        DetectedFileCount = 0;

        if (!Directory.Exists(path))
        {
            SetFolderMissing();
            return;
        }

        var count = 0;
        try
        {
            var scanService = new SourceScanService(new FileSystemScanner(), new MetadataReader(_settings.VideoTimeIsLocal));
            await foreach (var chunk in scanService.ScanAsync(path, cancellationToken: ct))
            {
                // A newer scan may have cleared the list while this chunk was read.
                ct.ThrowIfCancellationRequested();
                if (count == 0 && chunk.FirstOrDefault(f => f.Metadata?.CameraModel is not null)?.Metadata is { } first)
                    _settings.UpdateDetectedCamera(first.CameraModel!, first.CameraSerialNumber);

                var items = chunk.Select(f => new TransferFileItem
                {
                    FileName = f.File.FileName,
                    Status = FileTransferStatus.Pending,
                    SizeBytes = f.File.SizeBytes,
                    CaptureDate = f.CaptureDate,
                    SourcePath = f.File.FullPath,
                    Metadata = f.Metadata,
                }).ToList();
                _queue.AddFiles(items);
                FilesAdded?.Invoke(items);
                count += chunk.Count;
            }

            ct.ThrowIfCancellationRequested();
            _queue.RebuildDateGroups();
            ScanCompleted?.Invoke(this, EventArgs.Empty);
            DetectedFileCount = count;
            IsSourceScanning = false;
            SourceFolderExists = true;
            NotifySourceChanged();

            if (_queue.Files.Count > 0)
                await _settings.SetPreviewFileAsync(_queue.Files[0].SourcePath);
        }
        catch (OperationCanceledException)
        {
            // Source changed while scanning
        }
        catch (DirectoryNotFoundException)
        {
            SetFolderMissing();
        }
    }

    private void SetFolderMissing()
    {
        IsSourceScanning = false;
        SourceFolderExists = false;
        DetectedFileCount = 0;
        NotifySourceChanged();
    }

    private void AddRecentSourcePath(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        var existing = RecentSourcePaths.FirstOrDefault(
            p => p.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            RecentSourcePaths.Remove(existing);

        RecentSourcePaths.Insert(0, folderPath);
        while (RecentSourcePaths.Count > MaxRecentSourcePaths)
            RecentSourcePaths.RemoveAt(RecentSourcePaths.Count - 1);

        OnPropertyChanged(nameof(HasRecentSourcePaths));
        PersistSourceHistory(folderPath);
    }

    private void PersistSourceHistory(string selectedPath) =>
        _settings.SetSourceHistory(
            selectedPath,
            RecentSourcePaths.Where(Directory.Exists).Take(MaxRecentSourcePaths));

    private void NotifySourceChanged()
    {
        OnPropertyChanged(nameof(SelectedSourcePath));
        OnPropertyChanged(nameof(DisplaySourcePath));
        OnPropertyChanged(nameof(SourceStatusText));
        OnPropertyChanged(nameof(HasSelectedSourcePath));
        RescanSourceCommand.NotifyCanExecuteChanged();
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsViewModel.IsAutoDetect)
                           or nameof(SettingsViewModel.IsCameraUsb)
                           or nameof(SettingsViewModel.IsCardReader))
        {
            RefreshDrives();
        }
        else if (e.PropertyName is nameof(SettingsViewModel.VideoTimeIsLocal))
        {
            RescanSource();
        }
    }

    private void OnDriveChanged(object? sender, DriveChangedEventArgs e) => Dispatcher.UIThread.Post(RefreshDrives);

    private static string GetSourceDisplayName(string folderPath)
    {
        var trimmed = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrWhiteSpace(name) ? folderPath : name;
    }

    private static string ShortenPath(string path)
    {
        const int maxLength = 34;
        if (path.Length <= maxLength)
            return path;

        var root = Path.GetPathRoot(path) ?? string.Empty;
        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var leaf = Path.GetFileName(trimmed);
        var parent = Path.GetDirectoryName(trimmed);
        var parentName = string.IsNullOrWhiteSpace(parent)
            ? string.Empty
            : Path.GetFileName(parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        var result = string.IsNullOrWhiteSpace(parentName)
            ? $"{root}...\\{leaf}"
            : $"{root}{parentName}\\...\\{leaf}";

        return result.Length <= maxLength ? result : $"{path[..Math.Min(24, path.Length)]}...";
    }

    public void Dispose()
    {
        _settings.PropertyChanged -= OnSettingsPropertyChanged;
        _driveWatcher.DriveChanged -= OnDriveChanged;
        _driveWatcher.Dispose();
        // The running scan owns and disposes its source.
        _scanCts?.Cancel();
        GC.SuppressFinalize(this);
    }
}
