using ShootPorter.App.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using ShootPorter.App.Services;
using ShootPorter.Core.Cli;
using ShootPorter.Core.Dedup;
using ShootPorter.Core.Download;
using ShootPorter.Core.Plugins;
using ShootPorter.Core.Tokens;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Root ViewModel of the main window: profile selection, destination previews, download and custom action.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly TokenParser _tokenParser = new(new TokenRegistry());
    private readonly UpdateService _updateService = new();
    private CancellationTokenSource? _downloadCts;
    private CancellationTokenSource? _dedupCts;
    private bool _disposed;
    private bool _syncingProfile;
    private readonly CliOptions _startup;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private ProfileItem? _selectedProfile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConflicts))]
    private int _conflictCount;

    public MainWindowViewModel() : this(new CliOptions())
    {
    }

    /// <param name="startup">Command-line options (AutoPlay passes the inserted drive as <c>--source</c>).</param>
    public MainWindowViewModel(CliOptions startup)
    {
        _startup = startup;
        SourcePicker = new SourcePickerViewModel(SettingsViewModel, TransferQueueViewModel);
        SourcePicker.ScanCompleted += OnScanCompleted;
        SourcePicker.FilesAdded += OnFilesAdded;
        SettingsViewModel.PropertyChanged += OnSettingsPropertyChanged;
        SettingsViewModel.UpdateCheck = CheckForUpdateNowAsync;
        SelectedProfile = SettingsViewModel.SelectedProfile ?? Profiles.FirstOrDefault();

        _ = InitializeAsync();
    }

    public TransferQueueViewModel TransferQueueViewModel { get; } = new();
    public SettingsViewModel SettingsViewModel { get; } = new();
    public SourcePickerViewModel SourcePicker { get; }

    public ObservableCollection<ProfileItem> Profiles => SettingsViewModel.Profiles;

    public bool HasConflicts => ConflictCount > 0;

    /// <summary>
    /// Whether the custom action button should be visible in the transfer queue header.
    /// </summary>
    public bool ShowCustomButton => SettingsViewModel.EnableCustomButton;

    /// <summary>
    /// The caption for the custom action button, sourced from settings.
    /// </summary>
    public string CustomButtonCaption => SettingsViewModel.CustomButtonCaption;

    /// <summary>
    /// Callback set by the view layer to display the update dialog with a given ViewModel.
    /// </summary>
    public Action<UpdateViewModel>? ShowUpdateDialog { get; set; }

    /// <summary>
    /// Callback set by the view layer to ask what to do with a download target that already exists.
    /// </summary>
    public Func<string, Task<ExistingFileChoice>>? AskExistingFile { get; set; }

    private async Task InitializeAsync()
    {
        try
        {
            await SettingsViewModel.InitializeAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SelectedProfile = SettingsViewModel.SelectedProfile ?? Profiles.FirstOrDefault();
                SourcePicker.LoadSourceHistory();
                ApplyStartupOptions();
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error(ex, Resources.LogLoadingSettingsFailedDefaultsAreUsed);
        }

        await CheckForUpdateAsync();
    }

    private void ApplyStartupOptions()
    {
        if (_startup.ProfileName is { } profileName
            && Profiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase)) is { } profile)
            SelectedProfile = profile;
        else if (_startup.ProfileName is not null)
            Log.Warning(Resources.LogProfileFromTheCommandLineDoesNot, _startup.ProfileName);

        if (!string.IsNullOrWhiteSpace(_startup.JobCode))
            SettingsViewModel.JobCode = _startup.JobCode;

        if (!string.IsNullOrWhiteSpace(_startup.SourcePath))
            SourcePicker.AddCustomFolder(_startup.SourcePath);
    }

    private async Task CheckForUpdateAsync()
    {
        if (SettingsViewModel.CheckForUpdates)
            await CheckForUpdateNowAsync();
    }

    // Returns a status line for the settings page; shows the update dialog when a newer version exists.
    private async Task<string> CheckForUpdateNowAsync()
    {
        if (!_updateService.IsInstalled)
            return Resources.UpdateNotInstalled;

        try
        {
            var newVersion = await _updateService.CheckForUpdateAsync();
            if (newVersion is null)
                return string.Format(Resources.UpdateUpToDate, _updateService.CurrentVersion);

            await Dispatcher.UIThread.InvokeAsync(() =>
                ShowUpdateDialog?.Invoke(new UpdateViewModel(_updateService, _updateService.CurrentVersion, newVersion)));
            return string.Format(Resources.UpdateFound, newVersion);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or OperationCanceledException)
        {
            Log.Warning(ex, Resources.LogUpdateCheckFailed);
            return Resources.UpdateCheckFailed;
        }
    }

    partial void OnIsDownloadingChanged(bool value) => SourcePicker.IsLocked = value;

    partial void OnSelectedProfileChanged(ProfileItem? value)
    {
        if (value is null) return;

        SyncProfile(() => SettingsViewModel.SelectedProfile = value);
        RecomputeDestinationPreviews();
        TransferQueueViewModel.FilterFilesBySelectedDates();
    }

    // Profile selection is mirrored between this VM and the settings page; the flag stops the echo.
    private void SyncProfile(Action apply)
    {
        if (_syncingProfile)
            return;

        _syncingProfile = true;
        try
        {
            apply();
        }
        finally
        {
            _syncingProfile = false;
        }
    }

    private void OnScanCompleted(object? sender, EventArgs e) => RecomputeDestinationPreviews();

    // Previews while scanning; name clashes are detected once the whole source is known.
    private void OnFilesAdded(IReadOnlyList<TransferFileItem> files)
    {
        var offset = TransferQueueViewModel.AllFiles.Count - files.Count;
        var planned = PlanDestinations(files, offset);
        for (var i = 0; i < files.Count; i++)
            files[i].DestinationPreview = planned[i].Path;
    }

    private void RecomputeDestinationPreviews()
    {
        TransferQueueViewModel.DestinationPath = SelectedProfile?.Path ?? SettingsViewModel.DestinationRoot;
        var allFiles = TransferQueueViewModel.AllFiles;
        var planned = PlanDestinations(allFiles, 0);

        for (var i = 0; i < allFiles.Count; i++)
        {
            var file = allFiles[i];
            // A finished file shows where it went, not where it would go now.
            if (file.Status == FileTransferStatus.Completed)
                continue;
            file.DestinationPreview = planned[i].Path;
            if (planned[i].IsClash)
                file.Status = FileTransferStatus.NameClash;
            else if (file.Status == FileTransferStatus.NameClash)
                file.Status = FileTransferStatus.Pending;
        }

        TransferQueueViewModel.RecalculateStats();
        ConflictCount = TransferQueueViewModel.NameClashes;
        _ = MarkAlreadyDownloadedAsync();
    }

    // A file whose planned target already exists with the same size was downloaded before.
    private async Task MarkAlreadyDownloadedAsync()
    {
        _dedupCts?.Cancel();
        _dedupCts?.Dispose();
        if (IsDownloading)
        {
            _dedupCts = null;
            return;
        }

        var cts = _dedupCts = new CancellationTokenSource();
        var candidates = TransferQueueViewModel.AllFiles
            .Where(f => f.Status is FileTransferStatus.Pending or FileTransferStatus.AlreadyDownloaded)
            .Select(f => (File: f, f.SourcePath, f.DestinationPreview))
            .ToList();

        bool[] downloaded;
        try
        {
            downloaded = await Task.Run(() => candidates.Select(c => IsAlreadyDownloaded(c.SourcePath, c.DestinationPreview, cts.Token)).ToArray(), cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested)
            return;

        for (var i = 0; i < candidates.Count; i++)
        {
            var file = candidates[i].File;
            if (file.Status is not (FileTransferStatus.Pending or FileTransferStatus.AlreadyDownloaded))
                continue;
            file.Status = downloaded[i] ? FileTransferStatus.AlreadyDownloaded : FileTransferStatus.Pending;
        }

        TransferQueueViewModel.RecalculateStats();
    }

    private static bool IsAlreadyDownloaded(string sourcePath, string destinationPath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(destinationPath))
            return false;

        try
        {
            return new FileComparer().Compare(sourcePath, destinationPath).Status == FileStatus.Downloaded;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, Resources.LogCheckingForAnEarlierDownloadFailed, destinationPath);
            return false;
        }
    }

    private IReadOnlyList<PlannedDestination> PlanDestinations(IReadOnlyList<TransferFileItem> files, int firstIndex)
    {
        var destRoot = SelectedProfile?.Path ?? SettingsViewModel.DestinationRoot;
        var namingTemplate = SelectedProfile?.NamingTemplate ?? SettingsViewModel.DestinationTemplate;

        return DestinationPlanner.Plan(_tokenParser, namingTemplate, destRoot, files.Select((file, i) =>
            TokenContext.ForFile(
                file.SourcePath,
                file.Metadata,
                new DateTimeOffset(file.CaptureDate),
                SettingsViewModel.JobCode,
                SettingsViewModel.SequenceStart + firstIndex + i,
                SettingsViewModel.BuildCameraMappingsDictionary(file.Metadata?.CameraModel, file.Metadata?.CameraSerialNumber))));
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsViewModel.SelectedProfile):
                SyncProfile(() => SelectedProfile = SettingsViewModel.SelectedProfile);
                break;
            case nameof(SettingsViewModel.DestinationRoot)
                or nameof(SettingsViewModel.DestinationTemplate)
                or nameof(SettingsViewModel.JobCode)
                or nameof(SettingsViewModel.SequenceStart):
                RecomputeDestinationPreviews();
                break;
            case nameof(SettingsViewModel.EnableCustomButton):
                OnPropertyChanged(nameof(ShowCustomButton));
                break;
            case nameof(SettingsViewModel.CustomButtonCaption):
                OnPropertyChanged(nameof(CustomButtonCaption));
                break;
        }
    }

    [RelayCommand]
    private async Task StartDownloadAsync()
    {
        var pendingFiles = TransferQueueViewModel.AllFiles
            .Where(f => f.IsSelected && f.Status == FileTransferStatus.Pending)
            .ToList();

        if (pendingFiles.Count == 0)
            return;

        using var downloadCts = new CancellationTokenSource();
        _downloadCts = downloadCts;
        using var prompt = new ExistingFilePrompt(
            AskExistingFile is { } ask ? path => Dispatcher.UIThread.InvokeAsync(() => ask(path)) : null,
            downloadCts.Cancel);

        var options = new DownloadOptions
        {
            DestinationRoot = SelectedProfile?.Path ?? SettingsViewModel.DestinationRoot,
            MaxParallelism = SettingsViewModel.MaxParallelism,
            VerifyAfterCopy = SettingsViewModel.VerifyAfterCopy,
            BackupDestinations = BackupDestinationBuilder.FromPaths(SettingsViewModel.BackupPath1, SettingsViewModel.BackupPath2),
            AutoDeleteSource = SettingsViewModel.AutoDeleteSource,
            ResolveExistingFile = prompt.ResolveAsync,
        };

        IsDownloading = true;
        try
        {
            await new DownloadSession(TransferQueueViewModel, pendingFiles).RunAsync(options, downloadCts.Token);
            await RunPluginsAsync(pendingFiles, downloadCts.Token);
            await SettingsViewModel.RecordJobCodeUsedAsync();
            AdvanceSequence(pendingFiles);
        }
        finally
        {
            _downloadCts = null;
            IsDownloading = false;
        }
    }

    private async Task RunPluginsAsync(IReadOnlyList<TransferFileItem> batch, CancellationToken ct)
    {
        var plugins = SettingsViewModel.BuildPluginManager();
        if (plugins.Plugins.Count == 0)
            return;

        try
        {
            foreach (var file in batch.Where(f => f.Status == FileTransferStatus.Completed))
            {
                var path = file.DestinationPreview;
                var results = await Task.Run(() => plugins.RunAllAsync(path, ct), ct);
                var failed = results.Where(r => !r.Success).Select(r => r.PluginName).ToList();
                if (failed.Count > 0)
                {
                    Log.Warning(Resources.LogPostDownloadPluginsFailedFor, failed, path);
                    file.StatusDetail = string.Format(Resources.PluginsFailed, string.Join(", ", failed));
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log.Information(Resources.LogPostDownloadPluginsCancelled);
        }
    }

    // Numbers are assigned by queue position, so the next batch starts after the last number used.
    private void AdvanceSequence(IReadOnlyList<TransferFileItem> batch)
    {
        var batchSet = batch.ToHashSet();
        var allFiles = TransferQueueViewModel.AllFiles;
        for (var i = allFiles.Count - 1; i >= 0; i--)
        {
            if (allFiles[i].Status == FileTransferStatus.Completed && batchSet.Contains(allFiles[i]))
            {
                SettingsViewModel.SequenceStart += i + 1;
                return;
            }
        }
    }

    [RelayCommand]
    private async Task RunCustomActionAsync()
    {
        if (!SettingsViewModel.EnableCustomButton)
            return;

        var scriptPath = SettingsViewModel.CustomScriptPath;
        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            return;

        foreach (var file in TransferQueueViewModel.AllFiles.Where(f => f.Status == FileTransferStatus.Completed).ToList())
            await CustomScriptRunner.RunAsync(scriptPath, file.DestinationPreview);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SettingsViewModel.SaveSettingsNow();
        SettingsViewModel.PropertyChanged -= OnSettingsPropertyChanged;
        SourcePicker.ScanCompleted -= OnScanCompleted;
        SourcePicker.FilesAdded -= OnFilesAdded;
        SourcePicker.Dispose();
        // The running download owns and disposes its source.
        _downloadCts?.Cancel();
        _dedupCts?.Cancel();
        _dedupCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
