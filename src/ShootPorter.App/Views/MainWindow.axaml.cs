using SukiUI;
using SukiUI.Controls;

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
}
