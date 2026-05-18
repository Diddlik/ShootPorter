using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Phlow.App.ViewModels;

namespace Phlow.App.Views.Settings;

public partial class DownloadPathSettingsView : UserControl
{
    public DownloadPathSettingsView()
    {
        InitializeComponent();
    }

    private async void BrowseDestination_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Base Download Directory",
            AllowMultiple = false,
        });

        if (folders.Count > 0 && DataContext is SettingsViewModel vm)
        {
            var path = folders[0].TryGetLocalPath();
            if (path is not null)
            {
                vm.DestinationRoot = path;
            }
        }
    }

    private void ShowTokenHelp_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            _ = TokenHelpWindow.ShowAsync(owner);
        }
    }
}
