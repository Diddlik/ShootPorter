using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ShootPorter.App.ViewModels;

namespace ShootPorter.App.Views;

public partial class TokenHelpWindow : Window
{
    public TokenHelpWindow()
    {
        InitializeComponent();
    }

    public static Task ShowAsync(Window owner)
    {
        var window = new TokenHelpWindow
        {
            DataContext = new TokenHelpViewModel()
        };

        return window.ShowDialog(owner);
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
