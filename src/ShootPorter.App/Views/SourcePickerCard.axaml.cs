using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ShootPorter.App.ViewModels;

namespace ShootPorter.App.Views;

public partial class SourcePickerCard : UserControl
{
    public SourcePickerCard()
    {
        InitializeComponent();
    }

    private async void ChooseFolder_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Source Folder",
            AllowMultiple = false,
        });

        if (folders.Count == 0 || DataContext is not MainWindowViewModel vm)
            return;

        var path = folders[0].TryGetLocalPath();
        if (path is not null)
            vm.AddCustomFolder(path);
    }

    private void RecentSource_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string path } && DataContext is MainWindowViewModel vm)
        {
            vm.SelectRecentSource(path);
        }
    }
}
