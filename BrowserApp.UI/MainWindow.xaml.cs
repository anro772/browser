using System.Windows;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(
        MainViewModel viewModel,
        NavigationService navigationService,
        RequestInterceptor requestInterceptor,
        NetworkMonitorView networkMonitorView,
        IServiceProvider serviceProvider)
    {
        _viewModel = viewModel;
        _navigationService = navigationService;
        _requestInterceptor = requestInterceptor;
        _networkMonitorView = networkMonitorView;
        _serviceProvider = serviceProvider;

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

    private void RulesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ErrorLogger.LogInfo("Opening Rule Manager");
            var ruleManagerView = _serviceProvider.GetRequiredService<RuleManagerView>();
            ruleManagerView.Owner = this;
            ruleManagerView.ShowDialog();
            ErrorLogger.LogInfo("Rule Manager closed");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to open Rule Manager", ex);
        }
    }
}
