using ShootPorter.Core.Tokens;

namespace ShootPorter.Core.Tests.Tokens;

/// <summary>
/// Tests for <see cref="SequenceCounter"/> thread-safe sequence generation.
/// </summary>
public sealed class SequenceCounterTests
{
    [Fact]
    public void WhenCreatedWithDefaultThenStartsAtOne()
    {
        var counter = new SequenceCounter();
        Assert.Equal(1, counter.Current);
    }

    [Fact]
    public void WhenNextCalledThenReturnsCurrentAndIncrements()
    {
        var counter = new SequenceCounter();
        Assert.Equal(1, counter.Next());
        Assert.Equal(2, counter.Next());
        Assert.Equal(3, counter.Next());
        Assert.Equal(4, counter.Current);
    }

    [Fact]
    public void WhenResetCalledThenCounterRestartsAtValue()
    {
        var counter = new SequenceCounter();
        counter.Next();
        counter.Next();
        counter.Reset(10);
        Assert.Equal(10, counter.Current);
        Assert.Equal(10, counter.Next());
    }

    [Fact]
    public void WhenCalledFromMultipleThreadsThenNoValuesSkipped()
    {
        var counter = new SequenceCounter();
        const int threadCount = 100;
        var results = new int[threadCount];

        Parallel.For(0, threadCount, i =>
        {
            results[i] = counter.Next();
        });

        var sorted = results.Order().ToArray();
        for (var i = 0; i < threadCount; i++)
        {
            Assert.Equal(i + 1, sorted[i]);
        }
    }
}
