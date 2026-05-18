using ShootPorter.Core.Profiles;

namespace ShootPorter.Core.Tests.Profiles;

/// <summary>
/// Tests for <see cref="JobCodeHistory"/> covering recording, ordering, pruning, and persistence.
/// </summary>
public sealed class JobCodeHistoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public JobCodeHistoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "jobcodes.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task WhenLoadingNonExistentFileThenStartsEmpty()
    {
        var history = new JobCodeHistory(Path.Combine(_tempDir, "missing.json"));

        await history.LoadAsync();

        Assert.Empty(history.Entries);
    }

    [Fact]
    public void WhenRecordingJobCodeThenAppearsInEntries()
    {
        var history = new JobCodeHistory(_filePath);
        var date = new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);

        history.Record("WED-001", date);

        Assert.Single(history.Entries);
        Assert.Equal("WED-001", history.Entries[0].Code);
        Assert.Equal(date, history.Entries[0].UsedDate);
    }

    [Fact]
    public void WhenRecordingDuplicateJobCodeThenUpdatesDate()
    {
        var history = new JobCodeHistory(_filePath);
        var original = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updated  = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);

        history.Record("JOB-42", original);
        history.Record("JOB-42", updated);

        Assert.Single(history.Entries);
        Assert.Equal(updated, history.Entries[0].UsedDate);
    }

    [Fact]
    public void WhenGetRecentThenReturnsMostRecentFirst()
    {
        var history = new JobCodeHistory(_filePath);
        var t0 = DateTimeOffset.UtcNow;

        history.Record("OLDEST",  t0.AddDays(-3));
        history.Record("MIDDLE",  t0.AddDays(-2));
        history.Record("NEWEST",  t0.AddDays(-1));

        var recent = history.GetRecent(10);

        Assert.Equal(3, recent.Count);
        Assert.Equal("NEWEST", recent[0]);
        Assert.Equal("MIDDLE", recent[1]);
        Assert.Equal("OLDEST", recent[2]);
    }

    [Fact]
    public void WhenGetRecentWithMaxCountThenLimitsResults()
    {
        var history = new JobCodeHistory(_filePath);
        var t0 = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++)
            history.Record($"CODE-{i}", t0.AddHours(-i));

        var recent = history.GetRecent(3);

        Assert.Equal(3, recent.Count);
    }

    [Fact]
    public async Task WhenSavingWithExpiredEntriesThenPrunesOld()
    {
        // Use 30-day retention
        var history = new JobCodeHistory(_filePath, TimeSpan.FromDays(30));

        var now = DateTimeOffset.UtcNow;
        history.Record("FRESH", now.AddDays(-10));    // within 30 days — keep
        history.Record("STALE", now.AddDays(-45));    // older than 30 days — prune

        await history.SaveAsync();

        var history2 = new JobCodeHistory(_filePath, TimeSpan.FromDays(30));
        await history2.LoadAsync();

        Assert.Single(history2.Entries);
        Assert.Equal("FRESH", history2.Entries[0].Code);
    }

    [Fact]
    public async Task WhenSavingAndLoadingThenRoundTrips()
    {
        var history = new JobCodeHistory(_filePath);
        // Use a date within the default 60-day retention window
        var date = DateTimeOffset.UtcNow.AddDays(-5);
        history.Record("SPORTS-2025", date);
        await history.SaveAsync();

        var history2 = new JobCodeHistory(_filePath);
        await history2.LoadAsync();

        Assert.Single(history2.Entries);
        Assert.Equal("SPORTS-2025", history2.Entries[0].Code);
        Assert.Equal(date, history2.Entries[0].UsedDate);
    }
}
