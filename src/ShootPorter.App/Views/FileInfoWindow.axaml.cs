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

    public static async void ShowForFile(string filePath, bool videoTimeIsLocal, Window owner)
    {
        var viewModel = new FileInfoViewModel(videoTimeIsLocal);
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
