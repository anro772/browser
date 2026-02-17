using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views.Workspaces;

public partial class RulesWorkspaceView : UserControl
{
    public RulesWorkspaceView(RuleManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void RulesWorkspaceView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is RuleManagerViewModel viewModel)
        {
            await viewModel.LoadRulesCommand.ExecuteAsync(null);
        }
    }
}
