using ShootPorter.Core.Download;

namespace ShootPorter.Core.Tests.Download;

/// <summary>
/// Tests for <see cref="TransferProgressTracker"/> aggregation and throttling.
/// </summary>
public sealed class TransferProgressTrackerTests
{
    private sealed class ManualTime : TimeProvider
    {
        public long Ticks { get; set; }
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Ticks;
        public void Advance(TimeSpan by) => Ticks += by.Ticks;
    }

    private static DownloadProgress Progress(int fileIndex, long copied, long total = 100) =>
        new("src", "dst", copied, total, fileIndex, 2);

    [Fact]
    public void WhenFirstReportOfFileThenUpdateIsNotThrottled()
    {
        var tracker = new TransferProgressTracker(200, new ManualTime());

        var update = tracker.Report(Progress(1, 50));

        Assert.NotNull(update);
        Assert.Equal((0, 50, 25, true), (update.FileIndex, update.FilePercent, update.OverallPercent, update.IsStateChange));
    }

    [Fact]
    public void WhenIntermediateReportComesTooSoonThenItIsThrottled()
    {
        var tracker = new TransferProgressTracker(200, new ManualTime());
        tracker.Report(Progress(1, 10));

        Assert.Null(tracker.Report(Progress(1, 20)));
    }

    [Fact]
    public void WhenFileCompletesThenUpdateIsNotThrottled()
    {
        var tracker = new TransferProgressTracker(200, new ManualTime());
        tracker.Report(Progress(1, 10));

        var update = tracker.Report(Progress(1, 100));

        Assert.True(update?.IsStateChange);
    }

    [Fact]
    public void WhenEnoughTimePassedThenSpeedAndRemainingAreReported()
    {
        var time = new ManualTime();
        var tracker = new TransferProgressTracker(200, time);
        tracker.Report(Progress(1, 0));
        time.Advance(TimeSpan.FromSeconds(1));

        var update = tracker.Report(Progress(1, 50));

        Assert.Equal(50, update?.BytesPerSecond);
        Assert.Equal(TimeSpan.FromSeconds(3), update?.Remaining);
    }

    [Fact]
    public void WhenFilesAreMarkedFinishedThenFinishedCountIncreases()
    {
        var tracker = new TransferProgressTracker(200, new ManualTime());

        tracker.MarkFinished(0, 100);
        var finished = tracker.MarkFinished(1, 100);

        Assert.Equal(2, finished);
    }
}
