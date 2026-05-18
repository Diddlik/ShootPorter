using SukiUI.Controls;

namespace ShootPorter.App.Views;

/// <summary>
/// About dialog showing application info, description, and open source credits.
/// </summary>
public partial class AboutWindow : SukiWindow
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
