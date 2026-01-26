using System.Windows;
using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

/// <summary>
/// Interaction logic for PrivacyDashboardView.xaml
/// Minimal code-behind - all logic in PrivacyDashboardViewModel.
/// </summary>
public partial class PrivacyDashboardView : UserControl
{
    private readonly PrivacyDashboardViewModel _viewModel;

    /// <summary>
    /// Event raised when the View Rules button is clicked.
    /// </summary>
    public event EventHandler? ViewRulesRequested;

    /// <summary>
    /// Event raised when the Marketplace button is clicked.
    /// </summary>
    public event EventHandler? MarketplaceRequested;

    /// <summary>
    /// Event raised when the Channels button is clicked.
    /// </summary>
    public event EventHandler? ChannelsRequested;

    public PrivacyDashboardView(PrivacyDashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Refresh stats when view becomes visible
        await _viewModel.RefreshStatsCommand.ExecuteAsync(null);
    }

    private void ViewRulesButton_Click(object sender, RoutedEventArgs e)
    {
        ViewRulesRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MarketplaceButton_Click(object sender, RoutedEventArgs e)
    {
        MarketplaceRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ChannelsButton_Click(object sender, RoutedEventArgs e)
    {
        ChannelsRequested?.Invoke(this, EventArgs.Empty);
    }
}
