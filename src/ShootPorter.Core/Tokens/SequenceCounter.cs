namespace ShootPorter.Core.Tokens;

/// <summary>
/// Thread-safe counter that generates incrementing sequence numbers across a download session.
/// </summary>
public sealed class SequenceCounter
{
    private int _current;

    public SequenceCounter(int startValue = 1)
    {
        _current = startValue;
    }

    public int Current => Volatile.Read(ref _current);

    public int Next() => Interlocked.Increment(ref _current) - 1;

    public void Reset(int startValue = 1)
    {
        Interlocked.Exchange(ref _current, startValue);
    }
}
