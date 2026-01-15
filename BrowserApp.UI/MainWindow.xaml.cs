using Wpf.Ui.Controls;
using BrowserApp.UI.ViewModels;
using BrowserApp.UI.Services;
using BrowserApp.UI.Views;

namespace BrowserApp.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Minimal code-behind - all logic in MainViewModel.
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly NavigationService _navigationService;
    private readonly RequestInterceptor _requestInterceptor;
    private readonly NetworkMonitorView _networkMonitorView;

    public MainWindow(
        MainViewModel viewModel,
        NavigationService navigationService,
        RequestInterceptor requestInterceptor,
        NetworkMonitorView networkMonitorView)
    {
        _viewModel = viewModel;
        _navigationService = navigationService;
        _requestInterceptor = requestInterceptor;
        _networkMonitorView = networkMonitorView;

        InitializeComponent();

        DataContext = _viewModel;

        // Set the sidebar content
        SidebarContent.Content = _networkMonitorView;

        // Wire up WebView2 to NavigationService
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // Set the WebView2 control in the NavigationService
        _navigationService.SetWebView(webView);

        // Initialize navigation service (sets up WebView2 with UserDataFolder)
        await _navigationService.InitializeAsync();

        // Initialize request interceptor with CoreWebView2
        if (_navigationService.CoreWebView2 != null)
        {
            _requestInterceptor.SetCoreWebView2(_navigationService.CoreWebView2);
            await _requestInterceptor.InitializeAsync();
        }

        // Navigate to default page
        await _viewModel.HomeCommand.ExecuteAsync(null);
    }
}
