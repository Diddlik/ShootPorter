using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShootPorter.Core.Download;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// ViewModel for the download progress dashboard.
/// </summary>
public partial class DownloadViewModel : ViewModelBase
{
    private readonly DownloadOrchestrator _orchestrator = new();

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private string _currentFile = string.Empty;

    [ObservableProperty]
    private double _currentFileProgress;

    [ObservableProperty]
    private int _completedFiles;

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private int _failedFiles;

    [ObservableProperty]
    private string _statusText = "Ready to download.";

    public ObservableCollection<string> Log { get; } = [];

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartDownloadAsync(IReadOnlyList<(string Source, string Destination)>? filePairs, CancellationToken cancellationToken)
    {
        if (filePairs is null || filePairs.Count == 0)
        {
            StatusText = "No files to download.";
            return;
        }

        IsDownloading = true;
        TotalFiles = filePairs.Count;
        CompletedFiles = 0;
        FailedFiles = 0;
        OverallProgress = 0;
        Log.Clear();
        StatusText = "Downloading...";

        var progress = new Progress<DownloadProgress>(p =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                CurrentFile = Path.GetFileName(p.SourcePath);
                CurrentFileProgress = p.FileProgressFraction;
                OverallProgress = p.BatchProgressFraction;
            });
        });

        try
        {
            var options = new DownloadOptions { MaxParallelism = 2, VerifyAfterCopy = true };
            var result = await _orchestrator.DownloadBatchAsync(filePairs, options, progress, cancellationToken);

            CompletedFiles = result.SucceededCount;
            FailedFiles = result.FailedCount;
            OverallProgress = 1.0;

            foreach (var r in result.Results)
            {
                var msg = r.Success
                    ? $"OK: {Path.GetFileName(r.SourcePath)}"
                    : $"FAIL: {Path.GetFileName(r.SourcePath)} - {r.ErrorMessage}";
                Log.Add(msg);
            }

            StatusText = $"Done. {result.SucceededCount} succeeded, {result.FailedCount} failed in {result.Duration.TotalSeconds:F1}s.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Download cancelled.";
        }
        finally
        {
            IsDownloading = false;
        }
    }
}
