using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views.Workspaces;

public partial class ChannelsWorkspaceView : UserControl
{
    public ChannelsWorkspaceView(ChannelsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void ChannelsWorkspaceView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ChannelsViewModel viewModel)
        {
            await viewModel.LoadChannelsCommand.ExecuteAsync(null);
        }
    }
}
