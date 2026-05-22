using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
    private bool _extensionProfileWired;
    private DispatcherTimer? _sessionAutoSaveTimer;

    /// <summary>
    /// WebView2 environment pre-warm. We start <c>CoreWebView2Environment.CreateAsync</c>
    /// from the ctor (fire-and-forget) so the Chromium subprocess spawn overlaps with the
    /// window's first paint instead of running serially after it. MainWindow_Loaded awaits
    /// this same task before opening the first tab — InitializeAsync is idempotent so the
    /// awaiter just observes the cached result.
    /// </summary>
    private Task? _webViewWarmupTask;

    public MainWindow(
        MainViewModel viewModel,
        TabStripViewModel tabStrip,
        DownloadManagerViewModel downloadManagerViewModel,
        CopilotSidebarView copilotSidebarView,
        WorkspaceHostView workspaceHostView,
        INetworkLogger networkLogger,
        IServiceProvider serviceProvider)
    {
        _viewModel = viewModel;
        _tabStrip = tabStrip;
        _downloadManagerViewModel = downloadManagerViewModel;
        _copilotSidebarView = copilotSidebarView;
        _workspaceHostView = workspaceHostView;
        _networkLogger = networkLogger;
        _serviceProvider = serviceProvider;

        InitializeComponent();

        DataContext = _viewModel;

        // Eager-assign the always-present chrome: Copilot is the default visible sidebar
        // panel, WorkspaceHost is the overlay container (its inner workspace views are
        // themselves lazily resolved via WorkspaceHostView.WireLazyWorkspaces below).
        CopilotContent.Content = _copilotSidebarView;
        WorkspaceHostContainer.Content = _workspaceHostView;

        // Five non-default sidebar panels: defer until the user actually clicks their
        // tab. WireLazyContent attaches IsVisibleChanged once and self-detaches after
        // the first reveal. ~300-500ms saved off cold start.
        WireLazyContent(DashboardContent, sp => sp.GetRequiredService<PrivacyDashboardView>(),
            view =>
            {
                // Quick-action events live on PrivacyDashboardView. They couldn't be
                // wired in this ctor previously because the view didn't exist yet —
                // they're wired here on first reveal instead. Safe order-wise because
                // the dashboard must be visible before the user can click these.
                view.ViewRulesRequested += (s, e) => RulesButton_Click(this, new RoutedEventArgs());
                view.MarketplaceRequested += (s, e) => MarketplaceButton_Click(this, new RoutedEventArgs());
                view.ChannelsRequested += (s, e) => ChannelsButton_Click(this, new RoutedEventArgs());
                view.SettingsRequested += (s, e) =>
                {
                    // Open Settings, then on the next Loaded tick (after the workspace
                    // lazy-resolves SettingsWorkspaceView) scroll to the Privacy Mode card.
                    SettingsButton_Click(this, new RoutedEventArgs());
                    Dispatcher.BeginInvoke(
                        new Action(() => _workspaceHostView.RevealSettingsPrivacyModeSection()),
                        System.Windows.Threading.DispatcherPriority.Loaded);
                };
            });
        WireLazyContent(DownloadsContent,      sp => sp.GetRequiredService<DownloadManagerView>());
        WireLazyContent(NetworkMonitorContent, sp => sp.GetRequiredService<NetworkMonitorView>());
        WireLazyContent(HistoryContent,        sp => sp.GetRequiredService<HistoryView>());
        WireLazyContent(LogViewerContent,      sp => sp.GetRequiredService<LogViewerView>());

        // Workspace dialog views (Rules / Extensions / Marketplace / Channels / Profiles
        // / Settings) — same pattern, hooked from inside WorkspaceHostView since it owns
        // those ContentControls.
        _workspaceHostView.WireLazyWorkspaces(_serviceProvider);

        // Bookmarks bar: open-in-new-tab routes here since the new-tab plumbing lives on MainWindow.
        BookmarksBarControl.OpenInNewTabRequested += async (s, bookmark) =>
        {
            try { await _tabStrip.NewTabAsync(bookmark.Url); }
            catch (Exception ex) { ErrorLogger.LogError("Open bookmark in new tab failed", ex); }
        };

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
        _viewModel.PropertyChanged += OnMainViewModelPropertyChanged;

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

        // Kick off the WebView2 environment now so the Chromium subprocess spawns in
        // parallel with the first window paint. MainWindow_Loaded awaits the same task
        // before creating tabs, so it ends up free (or nearly so) by then.
        try
        {
            var profileService = _serviceProvider.GetRequiredService<ProfileService>();
            string userDataPath = profileService.GetUserDataPath();
            Directory.CreateDirectory(userDataPath);
            _webViewWarmupTask = _tabStrip.InitializeAsync(userDataPath);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("[MainWindow] WebView2 warmup kick-off failed", ex);
        }

    }

    private async void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsWorkspaceOpen))
        {
            // Capture before OnActiveTabChanged hides the WebView2 — once it's hidden
            // the underlying browser surface stops compositing and the capture returns
            // black. Awaiting here also avoids a one-frame flash of empty backdrop.
            if (_viewModel.IsWorkspaceOpen)
            {
                await CapturePageSnapshotAsync();
            }
            else
            {
                PageSnapshot.Source = null;
            }

            // Re-apply active tab visibility when workspace mode toggles.
            OnActiveTabChanged(this, _tabStrip.ActiveTab);
        }
    }

    /// <summary>
    /// Grabs a PNG of the current tab's WebView2 and shows it under the workspace scrim.
    /// Fails-silent: an empty / not-yet-navigated tab just leaves the snapshot null and
    /// the scrim falls back to solid dark — same as the old behaviour.
    /// </summary>
    private async Task CapturePageSnapshotAsync()
    {
        try
        {
            var tab = _tabStrip.ActiveTab;
            var coreWeb = tab?.WebView?.CoreWebView2;
            if (coreWeb == null)
            {
                PageSnapshot.Source = null;
                return;
            }

            using var stream = new MemoryStream();
            await coreWeb.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            stream.Position = 0;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = stream;
            bmp.EndInit();
            bmp.Freeze();
            PageSnapshot.Source = bmp;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to capture page snapshot for workspace overlay", ex);
            PageSnapshot.Source = null;
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Await the warmup kicked off from the ctor. InitializeAsync is idempotent
            // and returns immediately once the env exists, so this is essentially free
            // on the happy path. The fallback path covers the rare case where the
            // ctor's kick-off itself threw (the catch above will have logged).
            var profileService = _serviceProvider.GetRequiredService<ProfileService>();
            string userDataPath = profileService.GetUserDataPath();

            ErrorLogger.LogInfo($"[MainWindow] Initializing WebView2 with user data: {userDataPath}");

            if (_webViewWarmupTask != null)
            {
                await _webViewWarmupTask;
            }
            else
            {
                Directory.CreateDirectory(userDataPath);
                await _tabStrip.InitializeAsync(userDataPath);
            }

            ErrorLogger.LogInfo("[MainWindow] WebView2 environment ready");

            // Check for crash recovery (sentinel file exists = previous unclean shutdown)
            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            string sessionLockPath = Path.Combine(Path.GetDirectoryName(profileService.GetDatabasePath())!, "session.lock");
            bool crashDetected = File.Exists(sessionLockPath);
            bool restored = false;

            var settingsService = _serviceProvider.GetRequiredService<SettingsService>();
            var startupBehavior = settingsService.Settings.StartupBehavior;

            if (startupBehavior == StartupBehavior.RestoreSession)
            {
                // Always restore when setting is RestoreSession
                restored = await _tabStrip.RestoreSessionAsync(scopeFactory);
            }
            else if (crashDetected)
            {
                // Previous crash detected - ask user
                var result = System.Windows.MessageBox.Show(
                    "It looks like the browser didn't shut down properly last time.\nWould you like to restore your previous session?",
                    "Restore Session",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    restored = await _tabStrip.RestoreSessionAsync(scopeFactory);
                }
            }
            else if (startupBehavior != StartupBehavior.NewTab)
            {
                // Default: try to restore previous session
                restored = await _tabStrip.RestoreSessionAsync(scopeFactory);
            }

            if (!restored)
            {
                // No saved session or user declined — open first tab
                string homeUrl = !string.IsNullOrWhiteSpace(settingsService.Settings.HomePage)
                    ? settingsService.Settings.HomePage
                    : _serviceProvider.GetRequiredService<ISearchEngineService>().GetHomePageUrl();
                await _tabStrip.NewTabAsync(homeUrl);
            }

            // Start session auto-save timer (every 30 seconds)
            _sessionAutoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _sessionAutoSaveTimer.Tick += async (s, args) =>
            {
                try
                {
                    var sf = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
                    await _tabStrip.SaveSessionAsync(sf);
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError("[MainWindow] Auto-save session failed", ex);
                }
            };
            _sessionAutoSaveTimer.Start();

            ErrorLogger.LogInfo($"[MainWindow] Tab system initialized ({_tabStrip.Tabs.Count} tabs), auto-save active");

            // (Bookmark load was kicked off from the ctor — it should already be done
            // by now since it ran in parallel with WebView2 warmup + tab restoration.)
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
    /// Wires up network logging, download notifications, download tracking, and extensions.
    /// </summary>
    private async void OnTabReady(object? sender, BrowserTabItem tab)
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

        // Wire ExtensionService to the WebView2 profile on first tab init
        if (!_extensionProfileWired && tab.CoreWebView2 != null)
        {
            _extensionProfileWired = true;
            try
            {
                var extensionService = _serviceProvider.GetRequiredService<ExtensionService>();
                extensionService.SetProfile(tab.CoreWebView2.Profile);
                await extensionService.LoadAllEnabledAsync();
                await extensionService.EnsureBuiltInExtensionsAsync();
                ErrorLogger.LogInfo("[MainWindow] Extension service wired to WebView2 profile");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("[MainWindow] Failed to wire extension service", ex);
            }
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

        if (!_viewModel.IsWorkspaceOpen && activeTab?.WebView != null)
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

        // When workspace is open, keep browser surfaces hidden to avoid WebView2 airspace overlap.
        if (_viewModel.IsWorkspaceOpen)
        {
            HideNewTabPage();
            return;
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

    // Chrome-style: first click into an unfocused address bar selects the whole URL
    // so the user can immediately type or copy. The two-handler pattern is the canonical
    // WPF workaround — without PreviewMouseLeftButtonDown intercepting the click, the
    // caret would land mid-URL and the SelectAll would be undone before the mouse-up.
    private void AddressBar_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox tb)
        {
            tb.SelectAll();
        }
    }

    private void AddressBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox tb && !tb.IsKeyboardFocusWithin)
        {
            tb.Focus();
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

    /// <summary>
    /// Click outside the workspace dialog dismisses it (standard modal behavior).
    /// </summary>
    private void WorkspaceScrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _viewModel.CloseWorkspaceCommand.Execute(null);
        e.Handled = true;
    }

    /// <summary>
    /// Translates vertical mouse-wheel input into horizontal scrolling so the tab strip
    /// behaves like Chrome/Brave (no sideways scroll bar, wheel pans through tabs).
    /// </summary>
    private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
        e.Handled = true;
    }

    private void TabScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        UpdateTabStripOverflow();
    }

    private void TabScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTabStripOverflow();
    }

    /// <summary>
    /// Toggles the sticky "+" button based on whether the tab strip currently overflows.
    /// Runs at background priority so layout has settled before we read ScrollableWidth.
    /// </summary>
    private void UpdateTabStripOverflow()
    {
        Dispatcher.InvokeAsync(() =>
        {
            _tabStrip.IsTabStripOverflowing = TabScrollViewer.ScrollableWidth > 0.5;
        }, DispatcherPriority.Background);
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

    private void TabStripArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        // Walk up the visual tree from the hit element. If we find any interactive
        // control (tab, button, title bar) the click belongs to that control — bail out.
        DependencyObject? source = e.OriginalSource as DependencyObject;
        while (source != null && source != TabStripGrid)
        {
            if (source is System.Windows.Controls.Button ||
                source is Wpf.Ui.Controls.Button ||
                source is TabItemControl ||
                source is TitleBar)
            {
                return; // Let the child handle it
            }
            source = VisualTreeHelper.GetParent(source);
        }

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            e.Handled = true;
            return;
        }

        try
        {
            // Standard Windows behavior: dragging a maximized window restores it first,
            // positioning it so the cursor stays proportionally on the title bar.
            if (WindowState == WindowState.Maximized)
            {
                var mouse = PointToScreen(e.GetPosition(this));
                double proportionX = mouse.X / SystemParameters.PrimaryScreenWidth;

                WindowState = WindowState.Normal;

                Left = mouse.X - (Width * proportionX);
                Top = 0;
            }

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

    /// <summary>
    /// Lazily resolves a view from DI and assigns it to a <see cref="ContentControl"/>
    /// the first time that control becomes visible. Used to defer XAML parsing of
    /// sidebar panels that aren't visible on first paint — the user pays a small
    /// one-time cost on first reveal (typically &lt;250 ms) and never again. WPF's
    /// <see cref="UIElement.IsVisibleChanged"/> fires reliably when the bound
    /// visibility flips, and the handler self-detaches after the first hit so we
    /// don't re-resolve.
    /// </summary>
    private void WireLazyContent<T>(
        ContentControl host,
        Func<IServiceProvider, T> factory,
        Action<T>? onLoaded = null) where T : UIElement
    {
        DependencyPropertyChangedEventHandler? handler = null;
        handler = (_, _) =>
        {
            // Skip spurious IsVisibleChanged notifications from layout passes that
            // don't actually flip visibility, and the (impossible-in-practice) case
            // where Content was assigned by some other code path.
            if (!host.IsVisible || host.Content != null) return;

            var view = factory(_serviceProvider);
            host.Content = view;
            try { onLoaded?.Invoke(view); }
            catch (Exception ex) { ErrorLogger.LogError($"[MainWindow] Lazy-view onLoaded for {typeof(T).Name} failed", ex); }
            host.IsVisibleChanged -= handler;
        };
        host.IsVisibleChanged += handler;
    }
}
