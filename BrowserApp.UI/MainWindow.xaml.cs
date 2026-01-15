using Wpf.Ui.Controls;
using BrowserApp.UI.ViewModels;
using BrowserApp.UI.Services;

namespace BrowserApp.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Minimal code-behind - all logic in MainViewModel.
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly NavigationService _navigationService;

    public MainWindow(MainViewModel viewModel, NavigationService navigationService)
    {
        _viewModel = viewModel;
        _navigationService = navigationService;

        InitializeComponent();

        DataContext = _viewModel;

        // Wire up WebView2 to NavigationService
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // Set the WebView2 control in the NavigationService
        _navigationService.SetWebView(webView);

        // Initialize navigation service (sets up WebView2 with UserDataFolder)
        await _navigationService.InitializeAsync();

        // Navigate to default page
        await _viewModel.HomeCommand.ExecuteAsync(null);
    }
}
