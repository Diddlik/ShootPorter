using ShootPorter.Core.Discovery;

namespace ShootPorter.Core.Tests.Discovery;

/// <summary>
/// Tests for <see cref="DriveWatcherService"/> lifecycle behaviour.
/// </summary>
public sealed class DriveWatcherServiceTests
{
    [Fact]
    public void WhenStartAndStopWatchingThenNoException()
    {
        using var sut = new DriveWatcherService(pollInterval: TimeSpan.FromHours(1));

        var startEx = Record.Exception(() => sut.StartWatching());
        var stopEx = Record.Exception(() => sut.StopWatching());

        Assert.Null(startEx);
        Assert.Null(stopEx);
    }

    [Fact]
    public void WhenDisposedThenStopsWatching()
    {
        var sut = new DriveWatcherService(pollInterval: TimeSpan.FromHours(1));
        sut.StartWatching();

        var disposeEx = Record.Exception(() => sut.Dispose());

        Assert.Null(disposeEx);
    }

    [Fact]
    public void WhenStartWatchingAfterDisposeThenThrowsObjectDisposedException()
    {
        var sut = new DriveWatcherService(pollInterval: TimeSpan.FromHours(1));
        sut.Dispose();

        Assert.Throws<ObjectDisposedException>(() => sut.StartWatching());
    }

    [Fact]
    public void WhenDriveChangedEventSubscribedThenCanStartAndStop()
    {
        using var sut = new DriveWatcherService(pollInterval: TimeSpan.FromHours(1));
        var eventFired = false;
        sut.DriveChanged += (_, _) => eventFired = true;

        sut.StartWatching();
        sut.StopWatching();

        // No event expected to fire during this lifecycle test; just verify no exception
        Assert.False(eventFired);
    }
}
