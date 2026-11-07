using ShootPorter.App.ViewModels;
using ShootPorter.Core.Download;

namespace ShootPorter.App.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="ExistingFilePrompt"/>.
/// </summary>
public sealed class ExistingFilePromptTests
{
    [Fact]
    public async Task WhenUserChoosesOverwriteAllThenLaterConflictsAreNotAsked()
    {
        var asked = 0;
        using var prompt = new ExistingFilePrompt(_ => { asked++; return Task.FromResult(ExistingFileChoice.OverwriteAll); }, () => { });

        await prompt.ResolveAsync("a.jpg", CancellationToken.None);
        var second = await prompt.ResolveAsync("b.jpg", CancellationToken.None);

        Assert.Equal((1, ConflictResolution.Overwrite), (asked, second));
    }

    [Fact]
    public async Task WhenUserChoosesAddSuffixThenNextConflictIsAskedAgain()
    {
        var asked = 0;
        using var prompt = new ExistingFilePrompt(_ => { asked++; return Task.FromResult(ExistingFileChoice.AddSuffix); }, () => { });

        await prompt.ResolveAsync("a.jpg", CancellationToken.None);
        await prompt.ResolveAsync("b.jpg", CancellationToken.None);

        Assert.Equal(2, asked);
    }

    [Fact]
    public async Task WhenUserCancelsThenBatchIsCancelled()
    {
        var cancelled = false;
        using var prompt = new ExistingFilePrompt(_ => Task.FromResult(ExistingFileChoice.Cancel), () => cancelled = true);

        await Assert.ThrowsAsync<OperationCanceledException>(() => prompt.ResolveAsync("a.jpg", CancellationToken.None));
        Assert.True(cancelled);
    }

    [Fact]
    public async Task WhenNoDialogIsAvailableThenBatchIsCancelled()
    {
        var cancelled = false;
        using var prompt = new ExistingFilePrompt(null, () => cancelled = true);

        await Assert.ThrowsAsync<OperationCanceledException>(() => prompt.ResolveAsync("a.jpg", CancellationToken.None));
        Assert.True(cancelled);
    }
}
