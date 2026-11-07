using Avalonia.Interactivity;
using ShootPorter.App.ViewModels;
using SukiUI.Controls;

namespace ShootPorter.App.Views;

/// <summary>
/// Asks the user what to do with a download target that already exists. Closing the window cancels the download.
/// </summary>
public partial class ExistingFileDialog : SukiWindow
{
    public ExistingFileDialog()
    {
        InitializeComponent();
    }

    public ExistingFileDialog(string existingPath) : this()
    {
        PathText.Text = existingPath;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(ExistingFileChoice.Cancel);
    private void Overwrite_Click(object? sender, RoutedEventArgs e) => Close(ExistingFileChoice.Overwrite);
    private void OverwriteAll_Click(object? sender, RoutedEventArgs e) => Close(ExistingFileChoice.OverwriteAll);
    private void AddSuffix_Click(object? sender, RoutedEventArgs e) => Close(ExistingFileChoice.AddSuffix);
    private void AddSuffixAll_Click(object? sender, RoutedEventArgs e) => Close(ExistingFileChoice.AddSuffixAll);
}
