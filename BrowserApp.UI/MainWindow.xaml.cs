using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using Wpf.Ui.Controls;
using BrowserApp.Core.Interfaces;
using BrowserApp.UI.Controls;
using BrowserApp.UI.Models;
using BrowserApp.UI.Services;
using BrowserApp.UI.ViewModels;
using BrowserApp.UI.Views;

namespace BrowserApp.UI;

/// <summary>
/// Main browser window with tab strip, navigation bar, and content area.
/// Manages WebView2 instances in the visual tree as tabs are created/closed.
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly TabStripViewModel _tabStrip;
    private readonly NetworkMonitorView _networkMonitorView;
    private readonly LogViewerView _logViewerView;
    private readonly PrivacyDashboardView _dashboardView;
    private readonly HistoryView _historyView;
    private readonly IServiceProvider _serviceProvider;
    private readonly INetworkLogger _networkLogger;
    private bool _isFullScreen;
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private BrowserTabItem? _overlayTrackedTab;

    public MainWindow(
        MainViewModel viewModel,
        TabStripViewModel tabStrip,
        NetworkMonitorView networkMonitorView,
        LogViewerView logViewerView,
        PrivacyDashboardView dashboardView,
        HistoryView historyView,
        BookmarksPanel bookmarksPanel,
        INetworkLogger networkLogger,
        IServiceProvider serviceProvider)
    {
        _viewModel = viewModel;
        _tabStrip = tabStrip;
        _networkMonitorView = networkMonitorView;
        _logViewerView = logViewerView;
        _dashboardView = dashboardView;
        _historyView = historyView;
        _networkLogger = networkLogger;
        _serviceProvider = serviceProvider;

        InitializeComponent();

        DataContext = _viewModel;

        // Set the sidebar tab contents
        DashboardContent.Content = _dashboardView;
        BookmarksContent.Content = bookmarksPanel;
        NetworkMonitorContent.Content = _networkMonitorView;
        HistoryContent.Content = _historyView;
        LogViewerContent.Content = _logViewerView;

        // Wire up dashboard quick action events
        _dashboardView.ViewRulesRequested += (s, e) => RulesButton_Click(this, new RoutedEventArgs());
        _dashboardView.MarketplaceRequested += (s, e) => MarketplaceButton_Click(this, new RoutedEventArgs());
        _dashboardView.ChannelsRequested += (s, e) => ChannelsButton_Click(this, new RoutedEventArgs());

        // Wire up tab management events
        _tabStrip.TabAdded += OnTabAdded;
        _tabStrip.TabReady += OnTabReady;
        _tabStrip.TabRemoved += OnTabRemoved;
        _tabStrip.ActiveTabChanged += OnActiveTabChanged;

        // Wire up viewmodel events
        _viewModel.FocusAddressBarRequested += (s, e) =>
        {
            AddressBar.Focus();
            AddressBar.SelectAll();
        };

        _viewModel.ToggleFullScreenRequested += (s, e) => ToggleFullScreen();
        _viewModel.FindInPageRequested += (s, e) => OpenFindInPage();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Initialize the shared WebView2 environment using profile's user data path
            var profileService = _serviceProvider.GetRequiredService<ProfileService>();
            string userDataPath = profileService.GetUserDataPath();
            Directory.CreateDirectory(userDataPath);

            ErrorLogger.LogInfo($"[MainWindow] Initializing WebView2 with user data: {userDataPath}");

            await _tabStrip.InitializeAsync(userDataPath);

            ErrorLogger.LogInfo("[MainWindow] WebView2 environment created, creating first tab");

            // Open first tab and navigate to home
            await _tabStrip.NewTabAsync("https://www.google.com");

            ErrorLogger.LogInfo("[MainWindow] Tab system initialized with first tab");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("MainWindow_Loaded failed", ex);
            ErrorLogger.LogInfo($"[MainWindow] FATAL: Tab initialization failed: {ex.Message}");
        }
    }

    private async void ShowNewTabPage()
    {
        try
        {
            var newTabView = _serviceProvider.GetRequiredService<NewTabPageView>();
            var vm = newTabView.DataContext as NewTabPageViewModel;
            if (vm != null)
            {
                vm.SearchPerformed += (s, e) =>
                {
                    NewTabPageOverlay.Visibility = Visibility.Collapsed;
                };
            }
            NewTabPageOverlay.Content = newTabView;
            NewTabPageOverlay.Visibility = Visibility.Visible;
            await newTabView.LoadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to show new tab page: {ex.Message}");
        }
    }

    private void HideNewTabPage()
    {
        NewTabPageOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// When a new tab is created, add its WebView2 to the visual tree.
    /// Called BEFORE CoreWebView2 initialization (WebView2 needs an HWND first).
    /// </summary>
    private void OnTabAdded(object? sender, BrowserTabItem tab)
    {
        if (tab.WebView == null) return;

        // Add WebView2 to the host grid (hidden by default)
        // This gives it an HWND so EnsureCoreWebView2Async can succeed
        tab.WebView.Visibility = Visibility.Collapsed;
        WebViewHost.Children.Add(tab.WebView);
    }

    /// <summary>
    /// Called after CoreWebView2 is fully initialized on a tab.
    /// Wires up network logging and download notifications.
    /// </summary>
    private void OnTabReady(object? sender, BrowserTabItem tab)
    {
        // Wire this tab's request interceptor to the network logger
        if (tab.RequestInterceptor != null)
        {
            tab.RequestInterceptor.RequestCaptured += async (s, request) =>
            {
                await _networkLogger.LogRequestAsync(request);
            };
        }

        // Wire download notifications
        if (tab.CoreWebView2 != null)
        {
            DownloadNotificationControl.WireToWebView(tab.CoreWebView2);
        }
    }

    /// <summary>
    /// When a tab is removed, remove its WebView2 from the visual tree.
    /// </summary>
    private void OnTabRemoved(object? sender, BrowserTabItem tab)
    {
        if (tab.WebView != null && WebViewHost.Children.Contains(tab.WebView))
        {
            WebViewHost.Children.Remove(tab.WebView);
        }
    }

    /// <summary>
    /// When active tab changes, show its WebView2 and hide all others.
    /// </summary>
    private void OnActiveTabChanged(object? sender, BrowserTabItem? activeTab)
    {
        // Unsubscribe from previous tab's source changes (overlay tracking)
        if (_overlayTrackedTab != null)
        {
            _overlayTrackedTab.SourceChanged -= OnActiveTabSourceChangedForOverlay;
            _overlayTrackedTab = null;
        }

        foreach (var child in WebViewHost.Children)
        {
            if (child is Microsoft.Web.WebView2.Wpf.WebView2 wv)
            {
                wv.Visibility = Visibility.Collapsed;
            }
        }

        if (activeTab?.WebView != null)
        {
            activeTab.WebView.Visibility = Visibility.Visible;
        }

        // Show new tab page if the tab has no URL
        if (activeTab != null && string.IsNullOrEmpty(activeTab.Url))
        {
            ShowNewTabPage();

            // Subscribe to source changes so we hide the overlay when navigation starts
            _overlayTrackedTab = activeTab;
            activeTab.SourceChanged += OnActiveTabSourceChangedForOverlay;
        }
        else
        {
            HideNewTabPage();
        }
    }

    private void OnActiveTabSourceChangedForOverlay(object? sender, string newUrl)
    {
        if (!string.IsNullOrEmpty(newUrl))
        {
            HideNewTabPage();

            // Unsubscribe — no longer needed for this tab
            if (_overlayTrackedTab != null)
            {
                _overlayTrackedTab.SourceChanged -= OnActiveTabSourceChangedForOverlay;
                _overlayTrackedTab = null;
            }
        }
    }

    private void TabItem_Activated(object? sender, BrowserTabItem tab)
    {
        _tabStrip.ActivateTab(tab);
    }

    private async void TabItem_CloseRequested(object? sender, BrowserTabItem tab)
    {
        await _tabStrip.CloseTabAsync(tab);
    }

    private async void NewTabButton_Click(object sender, RoutedEventArgs e)
    {
        await _tabStrip.NewTabAsync();
    }

    private void ToggleFullScreen()
    {
        if (_isFullScreen)
        {
            WindowStyle = _previousWindowStyle;
            WindowState = _previousWindowState;
            _isFullScreen = false;
        }
        else
        {
            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;
        }
    }

    private void OpenFindInPage()
    {
        FindBarControl.Open(_tabStrip.ActiveTab?.WebView);
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

    private void MarketplaceButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ErrorLogger.LogInfo("Opening Marketplace");
            var marketplaceView = _serviceProvider.GetRequiredService<MarketplaceView>();
            marketplaceView.Owner = this;
            marketplaceView.ShowDialog();
            ErrorLogger.LogInfo("Marketplace closed");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to open Marketplace", ex);
        }
    }

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ErrorLogger.LogInfo("Opening Profile Selector");
            var profileView = _serviceProvider.GetRequiredService<ProfileSelectorView>();
            profileView.Owner = this;
            profileView.ShowDialog();
            ErrorLogger.LogInfo("Profile Selector closed");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to open Profile Selector", ex);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ErrorLogger.LogInfo("Opening Settings");
            var settingsView = _serviceProvider.GetRequiredService<SettingsView>();
            settingsView.Owner = this;
            settingsView.ShowDialog();
            ErrorLogger.LogInfo("Settings closed");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to open Settings", ex);
        }
    }
}
