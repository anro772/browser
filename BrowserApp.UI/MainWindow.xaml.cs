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
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly NavigationService _navigationService;
    private readonly RequestInterceptor _requestInterceptor;
    private readonly NetworkMonitorView _networkMonitorView;
    private readonly LogViewerView _logViewerView;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(
        MainViewModel viewModel,
        NavigationService navigationService,
        RequestInterceptor requestInterceptor,
        NetworkMonitorView networkMonitorView,
        LogViewerView logViewerView,
        IServiceProvider serviceProvider)
    {
        _viewModel = viewModel;
        _navigationService = navigationService;
        _requestInterceptor = requestInterceptor;
        _networkMonitorView = networkMonitorView;
        _logViewerView = logViewerView;
        _serviceProvider = serviceProvider;

        InitializeComponent();

        DataContext = _viewModel;

        // Set the sidebar tab contents
        NetworkMonitorContent.Content = _networkMonitorView;
        LogViewerContent.Content = _logViewerView;

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

            // Initialize CSS and JS injectors with CoreWebView2
            var cssInjector = _serviceProvider.GetRequiredService<CSSInjector>();
            cssInjector.SetCoreWebView2(_navigationService.CoreWebView2);

            var jsInjector = _serviceProvider.GetRequiredService<JSInjector>();
            jsInjector.SetCoreWebView2(_navigationService.CoreWebView2);

            System.Diagnostics.Debug.WriteLine("[MainWindow] CSS and JS injectors initialized");
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

    private void ChannelsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ErrorLogger.LogInfo("Opening Channels Manager");
            var channelsView = _serviceProvider.GetRequiredService<ChannelsView>();
            channelsView.Owner = this;
            channelsView.ShowDialog();
            ErrorLogger.LogInfo("Channels Manager closed");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to open Channels Manager", ex);
        }
    }
}
