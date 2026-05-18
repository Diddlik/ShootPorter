using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Phlow.App.ViewModels;

namespace Phlow.App.Views.Settings;

/// <summary>
/// Code-behind for custom actions settings providing file picker support.
/// </summary>
public partial class CustomActionsSettingsView : UserControl
{
    public CustomActionsSettingsView()
    {
        InitializeComponent();
    }

    public async void BrowseScript_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var window = this.FindAncestorOfType<Window>();
        if (window is null) return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Script File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Scripts") { Patterns = ["*.cmd", "*.bat", "*.ps1", "*.sh", "*.exe"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] }
            ]
        });

        if (files.Count > 0 && DataContext is SettingsViewModel vm)
        {
            var path = files[0].TryGetLocalPath();
            if (path is not null)
            {
                vm.CustomScriptPath = path;
            }
        }
    }
}
