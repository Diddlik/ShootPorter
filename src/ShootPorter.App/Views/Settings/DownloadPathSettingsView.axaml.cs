using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ShootPorter.App.ViewModels;

namespace ShootPorter.App.Views.Settings;

public partial class DownloadPathSettingsView : UserControl
{
    public DownloadPathSettingsView()
    {
        InitializeComponent();
    }

    private async void BrowseDestination_Click(object? sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync("Select Base Download Directory");
        if (path is not null && DataContext is SettingsViewModel vm)
            vm.DestinationRoot = path;
    }

    private async void BrowseBackup1_Click(object? sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync("Select Backup Directory 1");
        if (path is not null && DataContext is SettingsViewModel vm)
            vm.BackupPath1 = path;
    }

    private async void BrowseBackup2_Click(object? sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync("Select Backup Directory 2");
        if (path is not null && DataContext is SettingsViewModel vm)
            vm.BackupPath2 = path;
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    private void ShowTokenHelp_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            _ = TokenHelpWindow.ShowAsync(owner);
        }
    }
}
