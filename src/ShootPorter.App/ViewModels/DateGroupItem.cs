using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// A capture date with its file count; toggling it selects or deselects those files.
/// </summary>
public partial class DateGroupItem : ObservableObject
{
    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private bool _isSelected;

    public event EventHandler? DateSelectionChanged;

    public string DisplayDate => Date.ToString("dd MMM");

    partial void OnIsSelectedChanged(bool value)
    {
        DateSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Toggle() => IsSelected = !IsSelected;
}
