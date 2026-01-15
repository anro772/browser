using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

/// <summary>
/// Interaction logic for NetworkMonitorView.xaml
/// Minimal code-behind - all logic in NetworkMonitorViewModel.
/// </summary>
public partial class NetworkMonitorView : UserControl
{
    public NetworkMonitorView(NetworkMonitorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
