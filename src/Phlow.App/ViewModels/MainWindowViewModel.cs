using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Phlow.App.Services;
using Phlow.Core.Discovery;
using Phlow.Core.Download;
using Phlow.Core.Metadata;
using Phlow.Core.Tokens;

namespace Phlow.App.ViewModels;

/// <summary>
/// Root ViewModel managing page navigation for the main application shell.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int MaxRecentSourcePaths = 5;
    private readonly DriveWatcherService _driveWatcher = new();
    private readonly FileSystemScanner _scanner = new();
    private readonly IMetadataReader _metadataReader = new MetadataReader();
    private readonly TokenRegistry _tokenRegistry = new();
    private readonly TokenParser _tokenParser;
    private readonly UpdateService _updateService = new();
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _downloadCts;
    private bool _disposed;
    private bool _syncingProfile;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private string _currentPageTitle = "Transfer Queue";

    [ObservableProperty]
    private bool _isTransferQueueActive = true;

    [ObservableProperty]
    private bool _isPreferencesActive;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private string _deviceStatus = "No source selected";

    [ObservableProperty]
    private string _fileCountStatus = string.Empty;

    [ObservableProperty]
    private bool _isSourceScanning;

    [ObservableProperty]
    private bool _sourceFolderExists = true;

    [ObservableProperty]
    private int _detectedFileCount;

    [ObservableProperty]
    private InputSourceItem? _selectedInputSource;

    [ObservableProperty]
    private ProfileItem? _selectedProfile;

    [ObservableProperty]
    private int _conflictCount;

    private readonly TransferQueueViewModel _transferQueueViewModel = new();
    private readonly SettingsViewModel _settingsViewModel = new();

    public TransferQueueViewModel TransferQueueViewModel => _transferQueueViewModel;
    public SettingsViewModel SettingsViewModel => _settingsViewModel;

    public ObservableCollection<InputSourceItem> InputSources { get; } = [];
    public ObservableCollection<string> RecentSourcePaths { get; } = [];
    public ObservableCollection<ProfileItem> Profiles => _settingsViewModel.Profiles;

    public bool HasConflicts => ConflictCount > 0;
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

    /// <summary>
    /// Callback set by the view layer to display the update dialog with a given ViewModel.
    /// </summary>
    public Action<UpdateViewModel>? ShowUpdateDialog { get; set; }

    public MainWindowViewModel()
    {
        _tokenParser = new TokenParser(_tokenRegistry);
        _currentPage = _transferQueueViewModel;

        _settingsViewModel.PropertyChanged += OnSettingsPropertyChanged;

        if (_settingsViewModel.SelectedProfile is not null)
        {
            SelectedProfile = _settingsViewModel.SelectedProfile;
        }
        else if (Profiles.Count > 0)
        {
            SelectedProfile = Profiles[0];
        }

        _ = InitializeAsync();

        _driveWatcher.DriveChanged += OnDriveChanged;
        _driveWatcher.StartWatching();
        RefreshDrives();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _settingsViewModel.InitializeAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_settingsViewModel.SelectedProfile is not null)
                    SelectedProfile = _settingsViewModel.SelectedProfile;
                else if (Profiles.Count > 0)
                    SelectedProfile = Profiles[0];

                LoadSourceHistory();
            });
        }
        catch
        {
            // Settings load failure is non-fatal; defaults are used
        }

        await CheckForUpdateAsync();
    }

    private async Task CheckForUpdateAsync()
    {
        if (!_settingsViewModel.CheckForUpdates)
            return;

        try
        {
            var newVersion = await _updateService.CheckForUpdateAsync();
            if (newVersion is null)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var vm = new UpdateViewModel(
                    _updateService, _updateService.CurrentVersion, newVersion);
                ShowUpdateDialog?.Invoke(vm);
            });
        }
        catch
        {
            // Update check failure is non-fatal — no internet, rate-limited, etc.
        }
    }

    partial void OnSelectedInputSourceChanged(InputSourceItem? value)
    {
        if (value is null)
        {
            DeviceStatus = "No source selected";
            FileCountStatus = string.Empty;
            DetectedFileCount = 0;
            SourceFolderExists = true;
            IsSourceScanning = false;
            NotifySourcePickerChanged();
            return;
        }

        DeviceStatus = value.DisplayName;
        FileCountStatus = "Scanning...";
        DetectedFileCount = 0;
        SourceFolderExists = Directory.Exists(value.Path);
        PersistSourceHistory(value.Path);
        NotifySourcePickerChanged();
        _ = ScanSelectedSourceAsync();
    }

    partial void OnIsSourceScanningChanged(bool value)
    {
        OnPropertyChanged(nameof(SourceStatusText));
    }

    partial void OnSourceFolderExistsChanged(bool value)
    {
        OnPropertyChanged(nameof(SourceStatusText));
    }

    partial void OnDetectedFileCountChanged(int value)
    {
        OnPropertyChanged(nameof(SourceStatusText));
    }

    partial void OnConflictCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasConflicts));
    }

    partial void OnSelectedProfileChanged(ProfileItem? value)
    {
        if (value is null) return;

        if (!_syncingProfile)
        {
            _syncingProfile = true;
            try
            {
                _settingsViewModel.SelectedProfile = value;
            }
            finally
            {
                _syncingProfile = false;
            }
        }

        RecomputeDestinationPreviews();
        _transferQueueViewModel.FilterFilesBySelectedDates();
    }

    [RelayCommand]
    private void RefreshDrives()
    {
        var currentPath = SelectedInputSource?.Path;

        foreach (var old in InputSources.Where(s => s.IsRemovable).ToList())
        {
            InputSources.Remove(old);
        }

        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady);

            if (_settingsViewModel.IsCameraUsb)
            {
                // Camera USB mode: only show removable drives with a DCIM folder (camera convention)
                drives = drives.Where(d =>
                    d.DriveType == DriveType.Removable &&
                    Directory.Exists(Path.Combine(d.Name, "DCIM")));
            }
            else if (_settingsViewModel.IsCardReader)
            {
                // Card reader mode: all removable drives
                drives = drives.Where(d => d.DriveType == DriveType.Removable);
            }
            else
            {
                // Auto-detect mode: removable drives + fixed drives with DCIM folder
                drives = drives.Where(d =>
                    d.DriveType == DriveType.Removable ||
                    (d.DriveType == DriveType.Fixed && Directory.Exists(Path.Combine(d.Name, "DCIM"))));
            }

            foreach (var drive in drives)
            {
                var hasDcim = Directory.Exists(Path.Combine(drive.Name, "DCIM"));
                var label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                    ? "Removable Disk"
                    : drive.VolumeLabel;
                var icon = hasDcim ? "📷" : "💾";

                InputSources.Add(new InputSourceItem
                {
                    Path = drive.Name,
                    DisplayName = $"{label} ({drive.Name.TrimEnd('\\')})",
                    Icon = icon,
                    IsRemovable = true,
                    TotalSizeBytes = drive.TotalSize,
                    AvailableSizeBytes = drive.AvailableFreeSpace,
                });
            }
        }
        catch
        {
            // Drive enumeration can fail on restricted environments
        }

        if (currentPath is not null)
        {
            SelectedInputSource = InputSources.FirstOrDefault(
                                  s => s.Path.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
                                  ?? SelectedInputSource;
        }

        if (SelectedInputSource is null && InputSources.Count > 0)
        {
            SelectedInputSource = InputSources[0];
        }
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

    private void SelectSourcePath(string folderPath, bool addToRecent)
    {
        var existing = InputSources.FirstOrDefault(
            s => s.Path.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
        var sourceWasAlreadySelected = existing is not null && existing == SelectedInputSource;

        if (existing is not null)
        {
            SelectedInputSource = existing;
        }
        else
        {
            var item = new InputSourceItem
            {
                Path = folderPath,
                DisplayName = GetSourceDisplayName(folderPath),
                Icon = "📁",
                IsRemovable = false,
            };

            InputSources.Add(item);
            SelectedInputSource = item;
        }

        if (addToRecent)
            AddRecentSourcePath(folderPath);

        if (sourceWasAlreadySelected)
            RescanSource();
    }

    [RelayCommand]
    private void RescanSource()
    {
        if (string.IsNullOrWhiteSpace(SelectedSourcePath))
            return;

        SourceFolderExists = Directory.Exists(SelectedSourcePath);
        if (!SourceFolderExists)
        {
            DetectedFileCount = 0;
            _transferQueueViewModel.ClearFiles();
            NotifySourcePickerChanged();
            return;
        }

        FileCountStatus = "Scanning...";
        _ = ScanSelectedSourceAsync();
    }

    private async Task ScanSelectedSourceAsync()
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;
        var path = SelectedInputSource?.Path;

        if (string.IsNullOrWhiteSpace(path))
            return;

        _transferQueueViewModel.ClearFiles();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsSourceScanning = true;
            SourceFolderExists = Directory.Exists(path);
            DetectedFileCount = 0;
            NotifySourcePickerChanged();
        });

        if (!Directory.Exists(path))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsSourceScanning = false;
                SourceFolderExists = false;
                FileCountStatus = "Folder not found";
                NotifySourcePickerChanged();
            });
            return;
        }

        var profile = SelectedProfile;
        var destRoot = profile?.Path ?? _settingsViewModel.DestinationRoot;
        var namingTemplate = profile?.NamingTemplate ?? _settingsViewModel.DestinationTemplate;

        var count = 0;
        try
        {
            await foreach (var file in _scanner.ScanDirectoryAsync(path, recursive: true, ct))
            {
                FileMetadata? metadata = null;
                try
                {
                    metadata = await _metadataReader.ReadMetadataAsync(file.FullPath, ct);
                }
                catch
                {
                    // Metadata extraction can fail for unsupported or corrupt files
                }

                var captureDate = metadata?.CaptureDateTime?.DateTime
                                  ?? File.GetCreationTime(file.FullPath);
                var captureDateOffset = new DateTimeOffset(captureDate);
                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                var sourceFolderName = Path.GetDirectoryName(file.FullPath) is { } dir
                    ? Path.GetFileName(dir)
                    : string.Empty;

                // Update detected camera from the first file with camera info
                if (count == 0 && metadata?.CameraModel is not null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        _settingsViewModel.UpdateDetectedCamera(metadata.CameraModel, metadata.CameraSerialNumber));
                }

                var cameraMappings = _settingsViewModel.BuildCameraMappingsDictionary(
                    metadata?.CameraModel, metadata?.CameraSerialNumber);

                var context = new TokenContext(
                    CaptureDateTime: captureDateOffset,
                    OriginalFileName: originalFileName,
                    Extension: file.Extension,
                    JobCode: _settingsViewModel.JobCode,
                    SequenceNumber: count + 1,
                    CustomTokens: new Dictionary<string, string>())
                {
                    ImageNumber = TokenContext.ExtractImageNumber(originalFileName),
                    SourceFolderName = sourceFolderName,
                    SourceFolderNumber = TokenContext.ExtractImageNumber(sourceFolderName),
                    CameraManufacturer = metadata?.CameraManufacturer,
                    CameraModel = metadata?.CameraModel,
                    CameraSerialNumber = metadata?.CameraSerialNumber,
                    IsoSpeed = metadata?.IsoSpeed,
                    Aperture = metadata?.Aperture,
                    ShutterSpeed = metadata?.ShutterSpeed,
                    Copyright = metadata?.Copyright,
                    Owner = metadata?.Artist,
                    CameraMappings = cameraMappings,
                };

                var generatedName = _tokenParser.Parse(namingTemplate, context);
                var destinationPath = Path.Combine(destRoot, generatedName + file.Extension);

                var transferItem = new TransferFileItem
                {
                    FileName = file.FileName,
                    Status = FileTransferStatus.Pending,
                    Progress = 0,
                    SizeBytes = file.SizeBytes,
                    CaptureDate = captureDate,
                    SourcePath = file.FullPath,
                    DestinationPreview = destinationPath,
                    Metadata = metadata
                };

                await Dispatcher.UIThread.InvokeAsync(() => _transferQueueViewModel.AddFile(transferItem));

                count++;
                if (count % 50 == 0)
                {
                    var c = count;
                    await Dispatcher.UIThread.InvokeAsync(() => FileCountStatus = $"{c} files found...");
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _transferQueueViewModel.RebuildDateGroups();
                RecomputeDestinationPreviews();
                DetectedFileCount = count;
                FileCountStatus = $"{count} files detected";
                IsSourceScanning = false;
                SourceFolderExists = true;
                NotifySourcePickerChanged();
            });

            if (_transferQueueViewModel.Files.Count > 0)
            {
                var firstFilePath = _transferQueueViewModel.Files[0].SourcePath;
                await _settingsViewModel.SetPreviewFileAsync(firstFilePath);
            }
        }
        catch (OperationCanceledException)
        {
            // Source changed while scanning
        }
        catch (DirectoryNotFoundException)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsSourceScanning = false;
                SourceFolderExists = false;
                DetectedFileCount = 0;
                FileCountStatus = "Folder not found";
                NotifySourcePickerChanged();
            });
        }
    }

    private void LoadSourceHistory()
    {
        RecentSourcePaths.Clear();
        foreach (var path in _settingsViewModel.RecentSourcePaths.Where(Directory.Exists).Take(MaxRecentSourcePaths))
        {
            RecentSourcePaths.Add(path);
        }

        OnPropertyChanged(nameof(HasRecentSourcePaths));

        var selectedSourcePath = _settingsViewModel.SelectedSourcePath;
        if (!string.IsNullOrWhiteSpace(selectedSourcePath))
        {
            SelectSourcePath(selectedSourcePath, addToRecent: false);
            return;
        }

        NotifySourcePickerChanged();
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

    private void PersistSourceHistory(string selectedPath)
    {
        _settingsViewModel.SetSourceHistory(
            selectedPath,
            RecentSourcePaths.Where(Directory.Exists).Take(MaxRecentSourcePaths));
    }

    private void NotifySourcePickerChanged()
    {
        OnPropertyChanged(nameof(SelectedSourcePath));
        OnPropertyChanged(nameof(DisplaySourcePath));
        OnPropertyChanged(nameof(SourceStatusText));
        OnPropertyChanged(nameof(HasSelectedSourcePath));
        RescanSourceCommand.NotifyCanExecuteChanged();
    }

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

    private void RecomputeDestinationPreviews()
    {
        var destRoot = SelectedProfile?.Path ?? _settingsViewModel.DestinationRoot;
        var namingTemplate = SelectedProfile?.NamingTemplate ?? _settingsViewModel.DestinationTemplate;
        _transferQueueViewModel.DestinationPath = destRoot;
        var allFiles = _transferQueueViewModel.AllFiles;
        var destinationCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < allFiles.Count; i++)
        {
            var file = allFiles[i];
            var extension = Path.GetExtension(file.FileName);
            var captureDateOffset = new DateTimeOffset(file.CaptureDate);
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var meta = file.Metadata;

            var cameraMappings = _settingsViewModel.BuildCameraMappingsDictionary(
                meta?.CameraModel, meta?.CameraSerialNumber);

            var context = new TokenContext(
                CaptureDateTime: captureDateOffset,
                OriginalFileName: originalFileName,
                Extension: extension,
                JobCode: _settingsViewModel.JobCode,
                SequenceNumber: i + 1,
                CustomTokens: new Dictionary<string, string>())
            {
                ImageNumber = TokenContext.ExtractImageNumber(originalFileName),
                SourceFolderName = file.SourceFolderName,
                SourceFolderNumber = TokenContext.ExtractImageNumber(file.SourceFolderName),
                CameraManufacturer = meta?.CameraManufacturer,
                CameraModel = meta?.CameraModel,
                CameraSerialNumber = meta?.CameraSerialNumber,
                IsoSpeed = meta?.IsoSpeed,
                Aperture = meta?.Aperture,
                ShutterSpeed = meta?.ShutterSpeed,
                Copyright = meta?.Copyright,
                Owner = meta?.Artist,
                CameraMappings = cameraMappings,
            };

            var generatedName = _tokenParser.Parse(namingTemplate, context);
            var destinationPath = Path.Combine(destRoot, generatedName + extension);
            file.DestinationPreview = destinationPath;

            destinationCounts.TryGetValue(destinationPath, out var count);
            destinationCounts[destinationPath] = count + 1;
        }

        var clashPaths = new HashSet<string>(
            destinationCounts.Where(kv => kv.Value > 1).Select(kv => kv.Key),
            StringComparer.OrdinalIgnoreCase);

        foreach (var file in allFiles)
        {
            if (clashPaths.Contains(file.DestinationPreview))
            {
                file.Status = FileTransferStatus.NameClash;
            }
            else if (file.Status == FileTransferStatus.NameClash)
            {
                file.Status = FileTransferStatus.Pending;
            }
        }

        _transferQueueViewModel.RecalculateStats();
        ConflictCount = _transferQueueViewModel.NameClashes;
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.SelectedProfile) && !_syncingProfile)
        {
            _syncingProfile = true;
            try
            {
                SelectedProfile = _settingsViewModel.SelectedProfile;
            }
            finally
            {
                _syncingProfile = false;
            }
        }
        else if (e.PropertyName is nameof(SettingsViewModel.DestinationRoot)
                                 or nameof(SettingsViewModel.DestinationTemplate))
        {
            RecomputeDestinationPreviews();
        }
        else if (e.PropertyName is nameof(SettingsViewModel.EnableCustomButton))
        {
            OnPropertyChanged(nameof(ShowCustomButton));
        }
        else if (e.PropertyName is nameof(SettingsViewModel.CustomButtonCaption))
        {
            OnPropertyChanged(nameof(CustomButtonCaption));
        }
        else if (e.PropertyName is nameof(SettingsViewModel.IsAutoDetect)
                                 or nameof(SettingsViewModel.IsCameraUsb)
                                 or nameof(SettingsViewModel.IsCardReader))
        {
            RefreshDrives();
        }
    }

    private void OnDriveChanged(object? sender, DriveChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshDrives);
    }

    [RelayCommand]
    private void NavigateTo(string pageTag)
    {
        IsTransferQueueActive = false;
        IsPreferencesActive = false;

        switch (pageTag)
        {
            case "TransferQueue":
                CurrentPage = _transferQueueViewModel;
                CurrentPageTitle = "Transfer Queue";
                IsTransferQueueActive = true;
                break;
            case "Preferences":
                CurrentPage = _settingsViewModel;
                CurrentPageTitle = "Preferences";
                IsPreferencesActive = true;
                break;
        }
    }

    [RelayCommand]
    private async Task StartDownloadAsync()
    {
        var pendingFiles = _transferQueueViewModel.AllFiles
            .Where(f => f.IsSelected && f.Status == FileTransferStatus.Pending)
            .ToList();

        if (pendingFiles.Count == 0)
            return;

        IsDownloading = true;
        _downloadCts?.Cancel();
        _downloadCts = new CancellationTokenSource();
        var ct = _downloadCts.Token;

        var filePairs = pendingFiles
            .Select(f => (f.SourcePath, f.DestinationPreview))
            .ToList();

        foreach (var file in pendingFiles)
            file.Status = FileTransferStatus.Downloading;

        _transferQueueViewModel.RecalculateStats();

        var totalBytesToDownload = pendingFiles.Sum(f => f.SizeBytes);
        var bytesCopiedPerFile = new ConcurrentDictionary<int, long>();
        var completedFileIndices = new ConcurrentDictionary<int, byte>();
        var downloadStartTimestamp = Stopwatch.GetTimestamp();
        long lastUiDispatchTimestamp = downloadStartTimestamp;

        _transferQueueViewModel.IsDownloadActive = true;
        _transferQueueViewModel.DownloadSpeedText = string.Empty;
        _transferQueueViewModel.EstimatedTimeRemainingText = string.Empty;

        var orchestrator = new DownloadOrchestrator();
        var options = new DownloadOptions
        {
            MaxParallelism = _settingsViewModel.MaxParallelism,
            VerifyAfterCopy = _settingsViewModel.VerifyAfterCopy,
        };

        IProgress<DownloadProgress> progress = new CallbackProgress<DownloadProgress>(p =>
        {
            bytesCopiedPerFile[p.FileIndex] = p.BytesCopied;

            var isFileComplete = p.BytesCopied >= p.TotalBytes && p.TotalBytes > 0;
            if (isFileComplete)
                completedFileIndices.TryAdd(p.FileIndex, 0);

            var now = Stopwatch.GetTimestamp();
            var last = Volatile.Read(ref lastUiDispatchTimestamp);
            if (!isFileComplete && Stopwatch.GetElapsedTime(last, now).TotalMilliseconds < 100)
                return;

            if (Interlocked.CompareExchange(ref lastUiDispatchTimestamp, now, last) != last && !isFileComplete)
                return;

            var batchFraction = p.BatchProgressFraction;
            var completedCount = completedFileIndices.Count;
            var sourcePath = p.SourcePath;
            var fileFraction = p.FileProgressFraction;
            var totalBytesCopied = bytesCopiedPerFile.Values.Sum();

            Dispatcher.UIThread.Post(() =>
            {
                var file = pendingFiles.FirstOrDefault(
                    f => f.SourcePath.Equals(sourcePath, StringComparison.OrdinalIgnoreCase));
                if (file is not null)
                    file.Progress = (int)(fileFraction * 100);

                _transferQueueViewModel.OverallProgressPercent = (int)(batchFraction * 100);
                _transferQueueViewModel.CompletedFiles = completedCount;

                var elapsed = Stopwatch.GetElapsedTime(downloadStartTimestamp);
                if (elapsed.TotalSeconds > 0.5)
                {
                    var speedBytesPerSec = totalBytesCopied / elapsed.TotalSeconds;
                    _transferQueueViewModel.DownloadSpeedText = FormatSpeed(speedBytesPerSec);

                    var remainingBytes = totalBytesToDownload - totalBytesCopied;
                    if (speedBytesPerSec > 0)
                    {
                        var eta = TimeSpan.FromSeconds(remainingBytes / speedBytesPerSec);
                        _transferQueueViewModel.EstimatedTimeRemainingText = FormatTimeRemaining(eta);
                    }
                }
            });
        });

        try
        {
            var result = await orchestrator.DownloadBatchAsync(filePairs, options, progress, ct);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var r in result.Results)
                {
                    var file = pendingFiles.FirstOrDefault(
                        f => f.SourcePath.Equals(r.SourcePath, StringComparison.OrdinalIgnoreCase));
                    if (file is null) continue;

                    file.Status = r.Success ? FileTransferStatus.Completed : FileTransferStatus.Error;
                    file.Progress = r.Success ? 100 : 0;
                }

                _transferQueueViewModel.CompletedFiles = result.SucceededCount;
                _transferQueueViewModel.RecalculateStats();
            });
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var file in pendingFiles.Where(f => f.Status == FileTransferStatus.Downloading))
                    file.Status = FileTransferStatus.Pending;

                _transferQueueViewModel.RecalculateStats();
            });
        }
        finally
        {
            IsDownloading = false;
            _transferQueueViewModel.IsDownloadActive = false;
        }
    }

    [RelayCommand]
    private async Task RunCustomActionAsync()
    {
        if (!_settingsViewModel.EnableCustomButton)
            return;

        var scriptPath = _settingsViewModel.CustomScriptPath;
        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            return;

        var completedFiles = _transferQueueViewModel.AllFiles
            .Where(f => f.Status == FileTransferStatus.Completed)
            .ToList();

        if (completedFiles.Count == 0)
            return;

        foreach (var file in completedFiles)
        {
            var filePath = file.DestinationPreview;
            var directory = Path.GetDirectoryName(filePath) ?? string.Empty;

            var arguments = $"\"{filePath}\" \"{directory}\"";

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = scriptPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    },
                };

                process.Start();
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
            catch
            {
                // Script execution failure is non-fatal per file
            }
        }
    }

    /// <summary>
    /// Whether the custom action button should be visible in the transfer queue header.
    /// </summary>
    public bool ShowCustomButton => _settingsViewModel.EnableCustomButton;

    /// <summary>
    /// The caption for the custom action button, sourced from settings.
    /// </summary>
    public string CustomButtonCaption => _settingsViewModel.CustomButtonCaption;

    private static string FormatSpeed(double bytesPerSecond) => bytesPerSecond switch
    {
        < 1024 => $"{bytesPerSecond:F0} B/s",
        < 1024 * 1024 => $"{bytesPerSecond / 1024:F1} KB/s",
        < 1024 * 1024 * 1024 => $"{bytesPerSecond / (1024 * 1024):F1} MB/s",
        _ => $"{bytesPerSecond / (1024 * 1024 * 1024):F2} GB/s"
    };

    private static string FormatTimeRemaining(TimeSpan time) => time.TotalHours >= 1
        ? $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2} remaining"
        : $"{time.Minutes}:{time.Seconds:D2} remaining";

    private sealed class CallbackProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _ = _settingsViewModel.SaveSettingsNowAsync();
        _settingsViewModel.PropertyChanged -= OnSettingsPropertyChanged;
        _driveWatcher.DriveChanged -= OnDriveChanged;
        _driveWatcher.Dispose();
        _downloadCts?.Cancel();
        _downloadCts?.Dispose();
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
