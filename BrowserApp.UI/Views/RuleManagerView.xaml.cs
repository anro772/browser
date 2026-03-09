using System.Windows;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

/// <summary>
/// Interaction logic for RuleManagerView.xaml
/// </summary>
public partial class RuleManagerView : Wpf.Ui.Controls.FluentWindow
{
    public RuleManagerView(RuleManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Load rules when window opens
        Loaded += async (s, e) => await viewModel.LoadRulesCommand.ExecuteAsync(null);
    }
}
