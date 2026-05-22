using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    /// <summary>
    /// Event raised when the Mode tile (hero card at the top of the dashboard) is clicked.
    /// MainWindow opens the Settings workspace and scrolls to the Privacy Mode card.
    /// </summary>
    public event EventHandler? SettingsRequested;

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

    private void ViewRulesButton_Click(object sender, MouseButtonEventArgs e)
    {
        ViewRulesRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MarketplaceButton_Click(object sender, MouseButtonEventArgs e)
    {
        MarketplaceRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ChannelsButton_Click(object sender, MouseButtonEventArgs e)
    {
        ChannelsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ModeTile_Click(object sender, MouseButtonEventArgs e)
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }
}
