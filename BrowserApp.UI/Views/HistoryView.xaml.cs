using System.Windows;
using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

/// <summary>
/// Interaction logic for HistoryView.xaml
/// Minimal code-behind - all logic in HistoryViewModel.
/// </summary>
public partial class HistoryView : UserControl
{
    private readonly HistoryViewModel _viewModel;

    public HistoryView(HistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Load history when view becomes visible
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);
    }
}
