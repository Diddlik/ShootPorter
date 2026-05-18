using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootPorter.App.Services;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// ViewModel for the update notification and download progress dialog.
/// </summary>
public partial class UpdateViewModel : ViewModelBase
{
    private readonly UpdateService _updateService;
    private Action? _closeDialog;
    private CancellationTokenSource? _downloadCts;

    [ObservableProperty]
    private string _currentVersion = string.Empty;

    [ObservableProperty]
    private string _availableVersion = string.Empty;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private int _downloadProgress;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isReadyToInstall;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Whether the initial prompt buttons should be visible (not downloading, not yet downloaded).
    /// </summary>
    public bool ShowPromptButtons => !IsDownloading && !IsReadyToInstall;

    public UpdateViewModel(UpdateService updateService, string currentVersion, string availableVersion)
    {
        _updateService = updateService;
        CurrentVersion = currentVersion;
        AvailableVersion = availableVersion;
        StatusText = $"Version {availableVersion} is available. You are running {currentVersion}.";
    }

    /// <summary>
    /// Sets the callback to close the hosting dialog/window.
    /// </summary>
    public void SetCloseAction(Action closeAction) => _closeDialog = closeAction;

    partial void OnIsDownloadingChanged(bool value) => OnPropertyChanged(nameof(ShowPromptButtons));
    partial void OnIsReadyToInstallChanged(bool value) => OnPropertyChanged(nameof(ShowPromptButtons));

    [RelayCommand]
    private async Task DownloadUpdateAsync()
    {
        IsDownloading = true;
        HasError = false;
        StatusText = "Downloading update...";
        _downloadCts = new CancellationTokenSource();

        try
        {
            await _updateService.DownloadUpdateAsync(
                progress =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        DownloadProgress = progress;
                        StatusText = $"Downloading... {progress}%";
                    });
                },
                _downloadCts.Token);

            IsDownloading = false;
            IsReadyToInstall = true;
            StatusText = "Download complete. Ready to install.";
        }
        catch (OperationCanceledException)
        {
            IsDownloading = false;
            StatusText = "Download cancelled.";
        }
        catch (Exception ex)
        {
            IsDownloading = false;
            HasError = true;
            ErrorMessage = ex.Message;
            StatusText = "Download failed.";
        }
    }

    [RelayCommand]
    private void InstallAndRestart()
    {
        _updateService.ApplyUpdateAndRestart();
    }

    [RelayCommand]
    private void InstallOnExit()
    {
        _updateService.ApplyUpdateOnExit();
        _closeDialog?.Invoke();
    }

    [RelayCommand]
    private void SkipUpdate()
    {
        _downloadCts?.Cancel();
        _closeDialog?.Invoke();
    }
}
