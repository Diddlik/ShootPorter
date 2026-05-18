using System.Collections.Frozen;

namespace ShootPorter.Core.Discovery;

/// <summary>
/// Defines the set of image and video file extensions supported by ShootPorter.
/// </summary>
public static class SupportedFormats
{
    private static readonly FrozenSet<string> ImageExtensions = FrozenSet.ToFrozenSet(
        new[] { ".jpg", ".jpeg", ".tiff", ".tif", ".cr2", ".cr3", ".nef", ".arw", ".dng", ".raf", ".orf", ".pef", ".gpr" },
        StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> VideoExtensions = FrozenSet.ToFrozenSet(
        new[] { ".mov", ".mp4", ".avi", ".crm", ".mts", ".3gp", ".mxf" },
        StringComparer.OrdinalIgnoreCase);

    public static bool IsSupported(string extension) => IsImage(extension) || IsVideo(extension);

    public static bool IsImage(string extension) => ImageExtensions.Contains(extension);

    public static bool IsVideo(string extension) => VideoExtensions.Contains(extension);

    public static FileCategory? GetCategory(string extension)
    {
        if (IsImage(extension)) return FileCategory.Image;
        if (IsVideo(extension)) return FileCategory.Video;
        return null;
    }
}
