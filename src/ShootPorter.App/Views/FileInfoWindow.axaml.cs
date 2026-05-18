using Avalonia.Controls;
using Avalonia.Interactivity;
using ShootPorter.App.ViewModels;

namespace ShootPorter.App.Views;

public partial class FileInfoWindow : Window
{
    public FileInfoWindow()
    {
        InitializeComponent();
    }

    public static async void ShowForFile(string filePath, Window owner)
    {
        var viewModel = new FileInfoViewModel();
        var window = new FileInfoWindow
        {
            DataContext = viewModel
        };

        _ = viewModel.LoadMetadataAsync(filePath);
        await window.ShowDialog(owner);
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
