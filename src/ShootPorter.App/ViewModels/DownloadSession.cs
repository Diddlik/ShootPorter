using ShootPorter.App.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ShootPorter.Core.Download;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Runs one download batch and mirrors its progress and per-file results onto the transfer queue.
/// </summary>
internal sealed class DownloadSession
{
    private readonly TransferQueueViewModel _queue;
    private readonly IReadOnlyList<TransferFileItem> _files;
    private readonly Dictionary<string, int> _indexBySource;
    private readonly TransferProgressTracker _tracker;

    public DownloadSession(TransferQueueViewModel queue, IReadOnlyList<TransferFileItem> files)
    {
        _queue = queue;
        _files = files;
        _indexBySource = files
            .Select((f, i) => (f.SourcePath, i))
            .ToDictionary(x => x.SourcePath, x => x.i, StringComparer.OrdinalIgnoreCase);
        _tracker = new TransferProgressTracker(files.Sum(f => f.SizeBytes));
    }

    public async Task RunAsync(DownloadOptions options, CancellationToken ct)
    {
        _queue.OverallProgressPercent = 0;
        _queue.CompletedFiles = 0;
        _queue.DownloadSpeedText = string.Empty;
        _queue.EstimatedTimeRemainingText = string.Empty;
        _queue.IsDownloadActive = true;
        _queue.RecalculateStats();

        var filePairs = _files.Select(f => (f.SourcePath, f.DestinationPreview)).ToList();
        try
        {
            var result = await new DownloadOrchestrator().DownloadBatchAsync(
                filePairs,
                options,
                new CallbackProgress<DownloadProgress>(OnProgress),
                new CallbackProgress<DownloadResult>(OnFileCompleted),
                ct);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var r in result.Results)
                {
                    if (_indexBySource.TryGetValue(r.SourcePath, out var index))
                        ApplyResult(_files[index], r);
                }

                _queue.CompletedFiles = result.Results.Count;
                _queue.OverallProgressPercent = 100;
                _queue.RecalculateStats();
            });
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var file in _files.Where(f => f.Status == FileTransferStatus.Downloading))
                {
                    file.Status = FileTransferStatus.Pending;
                    file.Progress = 0;
                }

                _queue.RecalculateStats();
            });
        }
        finally
        {
            _queue.IsDownloadActive = false;
        }
    }

    private void OnProgress(DownloadProgress p)
    {
        if (_tracker.Report(p) is not { } update)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            var file = _files[update.FileIndex];
            if (file.Status == FileTransferStatus.Pending)
                file.Status = FileTransferStatus.Downloading;
            file.Progress = Math.Max(file.Progress, update.FilePercent);

            // Posts from parallel copies can arrive out of order; never move backwards.
            _queue.OverallProgressPercent = Math.Max(_queue.OverallProgressPercent, update.OverallPercent);

            if (update.IsStateChange)
                _queue.RecalculateStats();

            if (update.BytesPerSecond is { } speed)
                _queue.DownloadSpeedText = FormatSpeed(speed);
            if (update.Remaining is { } eta)
                _queue.EstimatedTimeRemainingText = FormatTimeRemaining(eta);
        });
    }

    private void OnFileCompleted(DownloadResult r)
    {
        if (!_indexBySource.TryGetValue(r.SourcePath, out var index))
            return;

        // Skipped files report no byte progress; count them as fully transferred.
        var finished = _tracker.MarkFinished(index, _files[index].SizeBytes);

        Dispatcher.UIThread.Post(() =>
        {
            ApplyResult(_files[index], r);
            _queue.CompletedFiles = Math.Max(_queue.CompletedFiles, finished);
            _queue.RecalculateStats();
        });
    }

    private static void ApplyResult(TransferFileItem file, DownloadResult result)
    {
        file.Status = result.Success ? FileTransferStatus.Completed : FileTransferStatus.Error;
        file.Progress = result.Success ? 100 : 0;
        file.SourceDisposition = result.SourceDisposition;
        file.StatusDetail = result.SourceDispositionMessage ?? result.ErrorMessage ?? string.Empty;
        if (result.Success)
            file.DestinationPreview = result.DestinationPath;
    }

    private static string FormatSpeed(double bytesPerSecond) => bytesPerSecond switch
    {
        < 1024 => $"{bytesPerSecond:F0} B/s",
        < 1024 * 1024 => $"{bytesPerSecond / 1024:F1} KB/s",
        < 1024 * 1024 * 1024 => $"{bytesPerSecond / (1024 * 1024):F1} MB/s",
        _ => $"{bytesPerSecond / (1024 * 1024 * 1024):F2} GB/s"
    };

    private static string FormatTimeRemaining(TimeSpan time) => time.TotalHours >= 1
        ? string.Format(Resources.TimeRemaining, $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}")
        : string.Format(Resources.TimeRemaining, $"{time.Minutes}:{time.Seconds:D2}");

    private sealed class CallbackProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }
}
