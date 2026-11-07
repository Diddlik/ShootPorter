using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShootPorter.App.ViewModels;
using ShootPorter.App.Views;

namespace ShootPorter.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            var viewModel = new MainWindowViewModel();
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
