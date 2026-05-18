using ShootPorter.App.ViewModels;
using SukiUI.Controls;

namespace ShootPorter.App.Views;

/// <summary>
/// Dialog window showing update availability, download progress, and install actions.
/// </summary>
public partial class UpdateDialog : SukiWindow
{
    public UpdateDialog()
    {
        InitializeComponent();
    }

    public UpdateDialog(UpdateViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.SetCloseAction(Close);
    }
}
