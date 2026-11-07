using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Represents a single camera-to-token mapping entry in the settings UI.
/// </summary>
public partial class CameraMappingItem : ObservableObject
{
    private readonly Action<CameraMappingItem>? _onDelete;

    [ObservableProperty]
    private string _cameraModel = string.Empty;

    [ObservableProperty]
    private string _t8Value = string.Empty;

    [ObservableProperty]
    private string _t9Value = string.Empty;

    public string MappingDisplay => $"{{T8}}={T8Value}  |  {{T9}}={T9Value}";

    public CameraMappingItem() { }

    public CameraMappingItem(Action<CameraMappingItem> onDelete)
    {
        _onDelete = onDelete;
    }

    [RelayCommand]
    private void Delete()
    {
        _onDelete?.Invoke(this);
    }
}
