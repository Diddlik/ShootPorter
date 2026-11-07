using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using ShootPorter.App.ViewModels;

namespace ShootPorter.App.Views;

public partial class TransferQueueView : UserControl
{
    public TransferQueueView()
    {
        InitializeComponent();
    }

    private void FilesDataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (FilesDataGrid.SelectedItem is TransferFileItem item && 
            !string.IsNullOrEmpty(item.SourcePath))
        {
            var window = this.FindAncestorOfType<Window>();
            if (window is not null)
            {
                var videoTimeIsLocal = window.DataContext is MainWindowViewModel main && main.SettingsViewModel.VideoTimeIsLocal;
                FileInfoWindow.ShowForFile(item.SourcePath, videoTimeIsLocal, window);
            }
        }
    }
}
