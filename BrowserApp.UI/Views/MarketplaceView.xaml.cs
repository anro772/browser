using System.Windows;
using Wpf.Ui.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

/// <summary>
/// Interaction logic for MarketplaceView.xaml
/// </summary>
public partial class MarketplaceView : FluentWindow
{
    public MarketplaceView()
    {
        InitializeComponent();
    }

    public MarketplaceView(MarketplaceViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private async void MarketplaceView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MarketplaceViewModel viewModel)
        {
            await viewModel.LoadRulesCommand.ExecuteAsync(null);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
