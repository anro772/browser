using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views.Workspaces;

public partial class ExtensionsWorkspaceView : UserControl
{
    public ExtensionsWorkspaceView(ExtensionManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void ExtensionsWorkspaceView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ExtensionManagerViewModel viewModel)
        {
            await viewModel.LoadExtensionsCommand.ExecuteAsync(null);
        }
    }
}
