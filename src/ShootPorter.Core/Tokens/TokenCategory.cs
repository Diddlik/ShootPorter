namespace ShootPorter.Core.Tokens;

/// <summary>
/// Categorises the kind of data a token resolves from.
/// </summary>
public enum TokenCategory
{
    DateTime,
    DateTimeNow,
    File,
    Sequence,
    Camera,
    Job,
    StringFunction,
    Custom,
}
