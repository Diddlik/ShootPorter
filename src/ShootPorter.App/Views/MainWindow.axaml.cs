using Avalonia.Platform.Storage;
using SukiUI;
using SukiUI.Controls;
using ShootPorter.App.ViewModels;

namespace ShootPorter.App.Views;

public partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ToggleTheme_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SukiTheme.GetInstance().SwitchBaseTheme();
    }

    private void About_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            DataContext = new ViewModels.AboutViewModel()
        };
        aboutWindow.ShowDialog(this);
    }

    public async void BrowseFolder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Source Folder",
            AllowMultiple = false,
        });

        if (folders.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            var path = folders[0].TryGetLocalPath();
            if (path is not null)
            {
                vm.AddCustomFolder(path);
            }
        }
    }
}

