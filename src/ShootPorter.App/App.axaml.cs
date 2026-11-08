using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SukiUI;
using SukiUI.Models;
using ShootPorter.App.ViewModels;
using ShootPorter.Core.Cli;
using ShootPorter.App.Views;

namespace ShootPorter.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Muted primary/accent instead of the saturated built-in blue.
        var soft = new SukiColorTheme("Soft", Color.Parse("#6A8BAE"), Color.Parse("#9DB4C9"));
        var suki = SukiTheme.GetInstance();
        suki.AddColorTheme(soft);
        suki.ChangeColorTheme(soft);
        // Switching light/dark re-applies the built-in color, so the soft one is set again.
        suki.OnBaseThemeChanged += _ => Avalonia.Threading.Dispatcher.UIThread.Post(() => suki.ChangeColorTheme(soft));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            var viewModel = new MainWindowViewModel(new CliParser().Parse(desktop.Args ?? []));
            viewModel.ShowUpdateDialog = vm =>
            {
                var dialog = new UpdateDialog(vm);
                dialog.ShowDialog(mainWindow);
            };
            viewModel.AskExistingFile = path =>
                new ExistingFileDialog(path).ShowDialog<ExistingFileChoice>(mainWindow);
            mainWindow.DataContext = viewModel;
            desktop.MainWindow = mainWindow;
            desktop.Exit += (_, _) => viewModel.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
