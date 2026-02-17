using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using Wpf.Ui.Controls;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.UI.Controls;
using BrowserApp.UI.Models;
using BrowserApp.UI.Services;
using BrowserApp.UI.ViewModels;
using BrowserApp.UI.Views;
using BrowserApp.UI.Views.Workspaces;

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
    private readonly DownloadManagerView _downloadManagerView;
    private readonly CopilotSidebarView _copilotSidebarView;
    private readonly WorkspaceHostView _workspaceHostView;
    private readonly IServiceProvider _serviceProvider;
    private readonly INetworkLogger _networkLogger;
    private readonly DownloadManagerViewModel _downloadManagerViewModel;
    private bool _isFullScreen;
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private BrowserTabItem? _overlayTrackedTab;
    private BrowserTabItem? _certTrackedTab;
    private readonly Dictionary<BrowserTabItem, EventHandler<NetworkRequest>> _requestCapturedHandlers = new();
    private readonly Dictionary<BrowserTabItem, CoreWebView2> _wiredWebViews = new();

    public MainWindow(
        MainViewModel viewModel,
        TabStripViewModel tabStrip,
        NetworkMonitorView networkMonitorView,
        LogViewerView logViewerView,
        PrivacyDashboardView dashboardView,
        HistoryView historyView,
        BookmarksPanel bookmarksPanel,
        DownloadManagerView downloadManagerView,
        DownloadManagerViewModel downloadManagerViewModel,
        CopilotSidebarView copilotSidebarView,
        WorkspaceHostView workspaceHostView,
        INetworkLogger networkLogger,
        IServiceProvider serviceProvider)
    {
        _viewModel = viewModel;
        _tabStrip = tabStrip;
        _networkMonitorView = networkMonitorView;
        _logViewerView = logViewerView;
        _dashboardView = dashboardView;
        _historyView = historyView;
        _downloadManagerView = downloadManagerView;
        _downloadManagerViewModel = downloadManagerViewModel;
        _copilotSidebarView = copilotSidebarView;
        _workspaceHostView = workspaceHostView;
        _networkLogger = networkLogger;
        _serviceProvider = serviceProvider;

        InitializeComponent();

        DataContext = _viewModel;

        // Set the sidebar tab contents
        CopilotContent.Content = _copilotSidebarView;
        DashboardContent.Content = _dashboardView;
        BookmarksContent.Content = bookmarksPanel;
        DownloadsContent.Content = _downloadManagerView;
        NetworkMonitorContent.Content = _networkMonitorView;
        HistoryContent.Content = _historyView;
        LogViewerContent.Content = _logViewerView;
        WorkspaceHostContainer.Content = _workspaceHostView;
        _workspaceHostView.SetWorkspaceContent(
            _serviceProvider.GetRequiredService<RulesWorkspaceView>(),
            _serviceProvider.GetRequiredService<ExtensionsWorkspaceView>(),
            _serviceProvider.GetRequiredService<MarketplaceWorkspaceView>(),
            _serviceProvider.GetRequiredService<ChannelsWorkspaceView>(),
            _serviceProvider.GetRequiredService<ProfilesWorkspaceView>(),
            _serviceProvider.GetRequiredService<SettingsWorkspaceView>());

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

        // Wire certificate warning bar events
        CertificateWarningBarControl.ProceedClicked += OnCertificateProceedClicked;
        CertificateWarningBarControl.GoBackClicked += OnCertificateGoBackClicked;

        // Wire download events to download manager (one-time registration)
        DownloadNotificationControl.DownloadStarted += (s, args) =>
        {
            RunBackgroundTask(
                _downloadManagerViewModel.AddDownload(
                    args.FileName,
                    args.SourceUrl,
                    args.DestinationPath,
                    args.TotalBytes),
                "Add download");
        };
        DownloadNotificationControl.DownloadProgressChanged += (s, args) =>
        {
            _downloadManagerViewModel.UpdateDownloadProgress(args.DestinationPath, args.ReceivedBytes);
        };
        DownloadNotificationControl.DownloadCompleted += (s, args) =>
        {
            RunBackgroundTask(
                _downloadManagerViewModel.CompleteDownload(args.DestinationPath, args.Success),
                "Complete download");
        };

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

            ErrorLogger.LogInfo("[MainWindow] WebView2 environment created");

            // Try to restore previous session
            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            bool restored = await _tabStrip.RestoreSessionAsync(scopeFactory);

            if (!restored)
            {
                // No saved session — open first tab and navigate to home
                var searchEngine = _serviceProvider.GetRequiredService<ISearchEngineService>();
                await _tabStrip.NewTabAsync(searchEngine.GetHomePageUrl());
            }

            ErrorLogger.LogInfo($"[MainWindow] Tab system initialized ({_tabStrip.Tabs.Count} tabs)");
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
    /// Wires up network logging, download notifications, and download tracking.
    /// </summary>
    private void OnTabReady(object? sender, BrowserTabItem tab)
    {
        // Wire this tab's request interceptor to the network logger (Bug 8: use stored handler)
        if (tab.RequestInterceptor != null)
        {
            EventHandler<NetworkRequest> handler = async (s, request) =>
            {
                await _networkLogger.LogRequestAsync(request);
            };
            _requestCapturedHandlers[tab] = handler;
            tab.RequestInterceptor.RequestCaptured += handler;
        }

        // Wire download notifications (Bug 9: track wired WebViews)
        if (tab.CoreWebView2 != null)
        {
            DownloadNotificationControl.WireToWebView(tab.CoreWebView2);
            _wiredWebViews[tab] = tab.CoreWebView2;
        }
    }

    /// <summary>
    /// When a tab is removed, remove its WebView2 from the visual tree.
    /// </summary>
    private void OnTabRemoved(object? sender, BrowserTabItem tab)
    {
        // Bug 8: Unsubscribe request interceptor handler
        if (tab.RequestInterceptor != null && _requestCapturedHandlers.TryGetValue(tab, out var handler))
        {
            tab.RequestInterceptor.RequestCaptured -= handler;
            _requestCapturedHandlers.Remove(tab);
        }

        // Bug 9: Unwire download notifications from this tab's WebView
        if (_wiredWebViews.TryGetValue(tab, out var coreWebView2))
        {
            DownloadNotificationControl.UnwireFromWebView(coreWebView2);
            _wiredWebViews.Remove(tab);
        }

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

        // Unsubscribe from previous tab's cert events
        if (_certTrackedTab != null)
        {
            _certTrackedTab.CertificateStatusChanged -= OnCertStatusChanged;
            _certTrackedTab.CertificateErrorDetected -= OnCertErrorDetected;
            _certTrackedTab = null;
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

        // Wire cert tracking for new active tab
        if (activeTab != null)
        {
            _certTrackedTab = activeTab;
            activeTab.CertificateStatusChanged += OnCertStatusChanged;
            activeTab.CertificateErrorDetected += OnCertErrorDetected;

            // Update cert warning bar state
            if (activeTab.CertificateStatus == CertificateStatus.Error)
            {
                CertificateWarningBarControl.Show(activeTab.CertificateErrorMessage);
            }
            else
            {
                CertificateWarningBarControl.Hide();
            }
        }
        else
        {
            CertificateWarningBarControl.Hide();
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

    private void OnCertStatusChanged(object? sender, EventArgs e)
    {
        if (_certTrackedTab != null)
        {
            if (_certTrackedTab.CertificateStatus == CertificateStatus.Error)
            {
                CertificateWarningBarControl.Show(_certTrackedTab.CertificateErrorMessage);
            }
            else
            {
                CertificateWarningBarControl.Hide();
            }
        }
    }

    private void OnCertErrorDetected(object? sender, CertificateErrorEventArgs e)
    {
        CertificateWarningBarControl.Show(_certTrackedTab?.CertificateErrorMessage ?? "Certificate error detected");
    }

    private void OnCertificateProceedClicked(object? sender, EventArgs e)
    {
        if (_certTrackedTab != null)
        {
            CertificateWarningBarControl.Hide();
            _certTrackedTab.ProceedPastCertificateError();
        }
    }

    private void OnCertificateGoBackClicked(object? sender, EventArgs e)
    {
        _tabStrip.ActiveTab?.GoBack();
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

    // Autocomplete keyboard navigation
    private void AddressBar_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_viewModel.IsSuggestionsOpen) return;

        if (e.Key == Key.Down)
        {
            if (SuggestionsListBox.SelectedIndex < SuggestionsListBox.Items.Count - 1)
            {
                SuggestionsListBox.SelectedIndex++;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            if (SuggestionsListBox.SelectedIndex > 0)
            {
                SuggestionsListBox.SelectedIndex--;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.CloseSuggestions();
            e.Handled = true;
        }
        else if (e.Key == Key.Return && SuggestionsListBox.SelectedItem is AutocompleteSuggestion suggestion)
        {
            _viewModel.AcceptSuggestionCommand.Execute(suggestion);
            e.Handled = true;
        }
    }

    private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
    {
        // Delay to allow click on popup item to register
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!SuggestionsListBox.IsMouseOver)
            {
                _viewModel.CloseSuggestions();
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void SuggestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SuggestionsListBox.SelectedItem is AutocompleteSuggestion suggestion && Mouse.LeftButton == MouseButtonState.Pressed)
        {
            _viewModel.AcceptSuggestionCommand.Execute(suggestion);
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

    private void OpenWorkspace(WorkspaceSection section)
    {
        _viewModel.OpenWorkspaceCommand.Execute(section);
    }

    private void ToolsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenWorkspace(WorkspaceSection.Rules);
    }

    private void RulesButton_Click(object sender, RoutedEventArgs e)
    {
        OpenWorkspace(WorkspaceSection.Rules);
    }

    private void ChannelsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenWorkspace(WorkspaceSection.Channels);
    }

    private void MarketplaceButton_Click(object sender, RoutedEventArgs e)
    {
        OpenWorkspace(WorkspaceSection.Marketplace);
    }

    private void ExtensionsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenWorkspace(WorkspaceSection.Extensions);
    }

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        OpenWorkspace(WorkspaceSection.Profiles);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenWorkspace(WorkspaceSection.Settings);
    }

    private void TitlebarDragRegion_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        try
        {
            DragMove();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to drag main window", ex);
        }
    }

    private static async void RunBackgroundTask(Task task, string operation)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"[MainWindow] {operation} failed", ex);
        }
    }
}
