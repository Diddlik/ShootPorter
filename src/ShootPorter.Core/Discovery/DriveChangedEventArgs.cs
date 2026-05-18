namespace ShootPorter.Core.Discovery;

/// <summary>
/// Event data raised when a removable drive is inserted or removed.
/// </summary>
public sealed record DriveChangedEventArgs(string DriveLetter, bool IsInserted);
