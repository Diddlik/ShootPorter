using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Caches immutable brushes so status getters do not allocate on every call.
/// </summary>
internal static class BrushCache
{
    private static readonly ConcurrentDictionary<string, IBrush> Cache = new();

    public static IBrush Get(string hex) =>
        Cache.GetOrAdd(hex, h => new ImmutableSolidColorBrush(Color.Parse(h)));
}
