using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Data.Converters;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// A download profile card (name, destination, naming template) with inline editing.
/// </summary>
public partial class ProfileItem : ObservableObject
{
    public static readonly IValueConverter BorderConverter = new ProfileBorderConverter();
    public static readonly IValueConverter ButtonTextConverter = new ProfileButtonTextConverter();
    public static readonly IValueConverter ButtonBgConverter = new ProfileButtonBgConverter();
    public static readonly IValueConverter ButtonFgConverter = new ProfileButtonFgConverter();

    private readonly Action<ProfileItem>? _onSelect;
    private readonly Action<ProfileItem>? _onDelete;
    private readonly Action<ProfileItem>? _onSave;

    private string _backupName = string.Empty;
    private string _backupPath = string.Empty;
    private string _backupNamingTemplate = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private string _namingTemplate = string.Empty;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _canDelete = true;

    [ObservableProperty]
    private bool _isEditing;

    public ProfileItem()
    {
    }

    public ProfileItem(Action<ProfileItem> onSelect, Action<ProfileItem> onDelete, Action<ProfileItem> onSave)
    {
        _onSelect = onSelect;
        _onDelete = onDelete;
        _onSave = onSave;
    }

    [RelayCommand]
    private void Select()
    {
        _onSelect?.Invoke(this);
    }

    [RelayCommand]
    private void Delete()
    {
        _onDelete?.Invoke(this);
    }

    [RelayCommand]
    private void Edit()
    {
        _backupName = Name;
        _backupPath = Path;
        _backupNamingTemplate = NamingTemplate;
        IsEditing = true;
    }

    [RelayCommand]
    private void SaveEdit()
    {
        IsEditing = false;
        _onSave?.Invoke(this);
    }

    [RelayCommand]
    private void CancelEdit()
    {
        Name = _backupName;
        Path = _backupPath;
        NamingTemplate = _backupNamingTemplate;
        IsEditing = false;
    }
}
