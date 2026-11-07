using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Keys of the status filter chips in the transfer queue.
/// </summary>
public static class TransferFilterKeys
{
    public const string All = "All";
    public const string Ready = "Ready";
    public const string Conflicts = "Conflicts";
    public const string Downloaded = "Downloaded";
    public const string Downloading = "Downloading";
    public const string Errors = "Errors";
}

/// <summary>
/// A status filter chip with its file count.
/// </summary>
public partial class TransferFilterChip : ObservableObject
{
    private readonly Action<TransferFilterChip> _onSelect;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private int _count;

    public TransferFilterChip(string key, string label, Action<TransferFilterChip> onSelect)
    {
        Key = key;
        Label = label;
        _onSelect = onSelect;
    }

    public string Key { get; }
    public string Label { get; }
    public string DisplayText => $"{Label} {Count}";

    public IBrush Background => IsActive ? BrushCache.Get("#2563eb") : BrushCache.Get("#111827");
    public IBrush BorderBrush => IsActive ? BrushCache.Get("#60a5fa") : BrushCache.Get("#2c384b");
    public IBrush Foreground => IsActive ? Brushes.White : BrushCache.Get("#cbd5e1");

    partial void OnIsActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(Background));
        OnPropertyChanged(nameof(BorderBrush));
        OnPropertyChanged(nameof(Foreground));
    }

    partial void OnCountChanged(int value)
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    [RelayCommand]
    private void Select() => _onSelect(this);
}
