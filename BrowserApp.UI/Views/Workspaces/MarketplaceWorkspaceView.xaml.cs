using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views.Workspaces;

public partial class MarketplaceWorkspaceView : UserControl
{
    public MarketplaceWorkspaceView(MarketplaceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void MarketplaceWorkspaceView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is MarketplaceViewModel viewModel)
        {
            await viewModel.LoadRulesCommand.ExecuteAsync(null);
        }
    }
}
