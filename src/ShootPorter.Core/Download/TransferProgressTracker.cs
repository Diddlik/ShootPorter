using System.Collections.Concurrent;

namespace ShootPorter.Core.Download;

/// <summary>
/// Aggregates per-file progress of a parallel download batch into overall progress, speed and ETA,
/// and throttles intermediate updates. Thread-safe.
/// </summary>
public sealed class TransferProgressTracker
{
    private static readonly TimeSpan Throttle = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MinElapsedForSpeed = TimeSpan.FromMilliseconds(500);

    private readonly long _totalBytes;
    private readonly TimeProvider _time;
    private readonly long _startTimestamp;
    private readonly ConcurrentDictionary<int, long> _bytesPerFile = new();
    private readonly ConcurrentDictionary<int, byte> _started = new();
    private readonly ConcurrentDictionary<int, byte> _completed = new();
    private long _lastUpdateTimestamp;
    private int _finishedCount;

    public TransferProgressTracker(long totalBytes, TimeProvider? time = null)
    {
        _totalBytes = Math.Max(1, totalBytes);
        _time = time ?? TimeProvider.System;
        _startTimestamp = _time.GetTimestamp();
        _lastUpdateTimestamp = _startTimestamp;
    }

    /// <summary>
    /// Records a progress report. Returns <c>null</c> when the update is throttled;
    /// the first and the final report of a file are never throttled.
    /// </summary>
    public TransferProgressUpdate? Report(DownloadProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        var fileIndex = progress.FileIndex - 1;
        _bytesPerFile[fileIndex] = progress.BytesCopied;

        var isFirstReport = _started.TryAdd(fileIndex, 0);
        var isFileComplete = progress.BytesCopied >= progress.TotalBytes && _completed.TryAdd(fileIndex, 0);
        var isStateChange = isFirstReport || isFileComplete;

        var now = _time.GetTimestamp();
        var last = Volatile.Read(ref _lastUpdateTimestamp);
        if (!isStateChange && _time.GetElapsedTime(last, now) < Throttle)
            return null;

        if (Interlocked.CompareExchange(ref _lastUpdateTimestamp, now, last) != last && !isStateChange)
            return null;

        return CreateUpdate(fileIndex, progress.FileProgressFraction, isStateChange, now);
    }

    /// <summary>
    /// Marks a file as finished (copied, skipped or failed) and counts it as fully transferred.
    /// Returns the number of finished files.
    /// </summary>
    public int MarkFinished(int fileIndex, long sizeBytes)
    {
        _bytesPerFile[fileIndex] = sizeBytes;
        return Interlocked.Increment(ref _finishedCount);
    }

    private TransferProgressUpdate CreateUpdate(int fileIndex, double fileFraction, bool isStateChange, long now)
    {
        var copied = _bytesPerFile.Values.Sum();
        var overall = (int)Math.Min(100, copied * 100 / _totalBytes);

        double? speed = null;
        TimeSpan? remaining = null;
        var elapsed = _time.GetElapsedTime(_startTimestamp, now);
        if (elapsed > MinElapsedForSpeed)
        {
            speed = copied / elapsed.TotalSeconds;
            if (speed > 0)
                remaining = TimeSpan.FromSeconds(Math.Max(0, _totalBytes - copied) / speed.Value);
        }

        return new TransferProgressUpdate(fileIndex, (int)(fileFraction * 100), overall, isStateChange, speed, remaining);
    }
}

/// <summary>
/// A snapshot of batch progress produced by <see cref="TransferProgressTracker"/>.
/// <paramref name="FileIndex"/> is zero-based.
/// </summary>
public sealed record TransferProgressUpdate(
    int FileIndex,
    int FilePercent,
    int OverallPercent,
    bool IsStateChange,
    double? BytesPerSecond,
    TimeSpan? Remaining);
