using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Models;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the main browser window.
/// Delegates navigation to the active tab via TabStripViewModel.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ISearchEngineService _searchEngineService;
    private readonly IBrowsingHistoryRepository _historyRepository;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TabStripViewModel _tabStrip;
    private readonly BookmarkViewModel _bookmarkViewModel;
    private readonly ProfileSelectorViewModel? _profileSelectorViewModel;
    private readonly SettingsService? _settingsService;
    private readonly ExtensionService? _extensionService;
    private bool _isDisposed;
    private DispatcherTimer? _debounceTimer;
    private CancellationTokenSource? _autocompleteCts;

    [ObservableProperty]
    private string _addressBarText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _pageTitle = "Privacy Browser";

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    [ObservableProperty]
    private string _statusBarText = "Ready";

    [ObservableProperty]
    private bool _isCurrentPageBookmarked;

    [ObservableProperty]
    private SidebarSection _selectedSidebarSection = SidebarSection.Copilot;

    [ObservableProperty]
    private WorkspaceSection _activeWorkspaceSection = WorkspaceSection.None;

    [ObservableProperty]
    private bool _isWorkspaceOpen;

    [ObservableProperty]
    private bool _isBookmarksBarVisible;

    // Autocomplete
    [ObservableProperty]
    private ObservableCollection<AutocompleteSuggestion> _suggestions = new();

    [ObservableProperty]
    private bool _isSuggestionsOpen;

    [ObservableProperty]
    private AutocompleteSuggestion? _selectedSuggestion;

    // Certificate status
    [ObservableProperty]
    private CertificateStatus _certificateStatus = CertificateStatus.Unknown;

    [ObservableProperty]
    private string _certificateErrorMessage = string.Empty;

    /// <summary>
    /// Live mirror of the built-in ad blocker (ABP) enabled state. Updated by
    /// <see cref="ExtensionService.AdBlockerStateChanged"/>. The active-profile pill
    /// shows a shield indicator when this is true.
    /// </summary>
    [ObservableProperty]
    private bool _isAdBlockerEnabled;

    public TabStripViewModel TabStrip => _tabStrip;
    public BookmarkViewModel BookmarkViewModel => _bookmarkViewModel;
    public ProfileSelectorViewModel? ProfileSelectorViewModel => _profileSelectorViewModel;
    public SettingsService? Settings => _settingsService;

    public bool CanGoBack => _tabStrip.ActiveTab?.CanGoBack ?? false;
    public bool CanGoForward => _tabStrip.ActiveTab?.CanGoForward ?? false;

    public MainViewModel(
        ISearchEngineService searchEngineService,
        IBrowsingHistoryRepository historyRepository,
        IServiceScopeFactory scopeFactory,
        TabStripViewModel tabStrip,
        BookmarkViewModel bookmarkViewModel,
        SettingsService? settingsService = null,
        ProfileSelectorViewModel? profileSelectorViewModel = null,
        ExtensionService? extensionService = null)
    {
        _searchEngineService = searchEngineService;
        _historyRepository = historyRepository;
        _scopeFactory = scopeFactory;
        _tabStrip = tabStrip;
        _bookmarkViewModel = bookmarkViewModel;
        _settingsService = settingsService;
        _profileSelectorViewModel = profileSelectorViewModel;
        _extensionService = extensionService;

        // Subscribe to active tab changes
        _tabStrip.ActiveTabChanged += OnActiveTabChanged;

        // Setup debounce timer for autocomplete (Bug 10: named handler for proper cleanup)
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _debounceTimer.Tick += OnDebounceTimerTick;

        // Restore the bookmarks-bar visibility from persisted settings.
        if (_settingsService != null)
        {
            _isBookmarksBarVisible = _settingsService.ShowBookmarksBar;
            _settingsService.ShowBookmarksBarChanged += OnShowBookmarksBarChanged;
        }

        // Mirror the built-in ad blocker state for the active-profile pill's shield
        // indicator. Initial value is set when EnsureBuiltInExtensionsAsync fires the
        // event after the first tab loads; this also catches user toggles in Settings.
        if (_extensionService != null)
        {
            _extensionService.AdBlockerStateChanged += OnAdBlockerStateChanged;
            _ = LoadInitialAdBlockerStateAsync();
        }
    }

    private async Task LoadInitialAdBlockerStateAsync()
    {
        if (_extensionService == null) return;
        try
        {
            IsAdBlockerEnabled = await _extensionService.IsAdBlockerEnabledAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("[MainViewModel] Failed to load initial ad blocker state", ex);
        }
    }

    private void OnAdBlockerStateChanged(object? sender, bool enabled)
    {
        // ExtensionService can fire on background threads (CRX install, WebView2 callbacks).
        // Marshal to the UI thread so the pill binding updates safely.
        if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
        {
            IsAdBlockerEnabled = enabled;
        }
        else
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => IsAdBlockerEnabled = enabled);
        }
    }

    private void OnShowBookmarksBarChanged(object? sender, bool value)
    {
        // Sync property without re-triggering the setter chain (setting only fires SaveSettings if changed).
        if (IsBookmarksBarVisible != value)
        {
            IsBookmarksBarVisible = value;
        }
    }

    /// <summary>
    /// Toggles the bookmarks bar (Ctrl+Shift+B). Persists across restarts.
    /// </summary>
    [RelayCommand]
    private void ToggleBookmarksBar()
    {
        IsBookmarksBarVisible = !IsBookmarksBarVisible;
        if (_settingsService != null)
        {
            _settingsService.ShowBookmarksBar = IsBookmarksBarVisible;
        }
    }

    private async void OnDebounceTimerTick(object? sender, EventArgs e)
    {
        _debounceTimer?.Stop();

        // Bug 6: Cancel previous autocomplete operation and create a new token
        _autocompleteCts?.Cancel();
        _autocompleteCts?.Dispose();
        _autocompleteCts = new CancellationTokenSource();
        var token = _autocompleteCts.Token;

        await UpdateSuggestionsAsync(AddressBarText, token);
    }

    private BrowserTabItem? _subscribedTab;
    private bool _suppressSuggestions;

    private void OnActiveTabChanged(object? sender, BrowserTabItem? tab)
    {
        // Bug 5: Close autocomplete popup on tab switch
        IsSuggestionsOpen = false;
        Suggestions.Clear();

        // Unsubscribe from previous tab
        if (_subscribedTab != null)
        {
            _subscribedTab.SourceChanged -= OnTabSourceChanged;
            _subscribedTab.NavigationStarting -= OnTabNavigationStarting;
            _subscribedTab.NavigationCompleted -= OnTabNavigationCompleted;
            _subscribedTab.TitleChanged -= OnTabTitleChanged;
            _subscribedTab.StatusBarTextChanged -= OnTabStatusBarTextChanged;
            _subscribedTab.CertificateStatusChanged -= OnTabCertificateStatusChanged;
        }

        _subscribedTab = tab;

        if (tab != null)
        {
            // Subscribe to new tab
            tab.SourceChanged += OnTabSourceChanged;
            tab.NavigationStarting += OnTabNavigationStarting;
            tab.NavigationCompleted += OnTabNavigationCompleted;
            tab.TitleChanged += OnTabTitleChanged;
            tab.StatusBarTextChanged += OnTabStatusBarTextChanged;
            tab.CertificateStatusChanged += OnTabCertificateStatusChanged;

            // Update UI to match active tab state
            _suppressSuggestions = true;
            AddressBarText = tab.Url;
            _suppressSuggestions = false;
            PageTitle = string.IsNullOrEmpty(tab.Title) || tab.Title == "New Tab"
                ? "Privacy Browser"
                : $"{tab.Title} - Privacy Browser";
            IsLoading = tab.IsLoading;
            CertificateStatus = tab.CertificateStatus;
            CertificateErrorMessage = tab.CertificateErrorMessage;
        }

        NotifyNavigationStateChanged();
    }

    private void OnTabSourceChanged(object? sender, string newUrl)
    {
        _suppressSuggestions = true;
        AddressBarText = newUrl;
        _suppressSuggestions = false;
        IsSuggestionsOpen = false;
        NotifyNavigationStateChanged();
    }

    private void OnTabNavigationStarting(object? sender, EventArgs e)
    {
        IsLoading = true;
    }

    private async void OnTabNavigationCompleted(object? sender, bool isSuccess)
    {
        IsLoading = false;
        NotifyNavigationStateChanged();

        if (isSuccess && _subscribedTab != null && !string.IsNullOrEmpty(_subscribedTab.Url))
        {
            // Update title
            var title = _subscribedTab.Title;
            PageTitle = string.IsNullOrEmpty(title) || title == "New Tab"
                ? "Privacy Browser"
                : $"{title} - Privacy Browser";

            // Record browsing history
            try
            {
                await _historyRepository.AddAsync(new BrowsingHistoryEntity
                {
                    Url = _subscribedTab.Url,
                    Title = _subscribedTab.Title,
                    VisitedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to record history: {ex.Message}");
            }
        }
    }

    private void OnTabTitleChanged(object? sender, string title)
    {
        PageTitle = string.IsNullOrEmpty(title) || title == "New Tab"
            ? "Privacy Browser"
            : $"{title} - Privacy Browser";
    }

    private void OnTabStatusBarTextChanged(object? sender, string text)
    {
        StatusBarText = string.IsNullOrEmpty(text) ? "Ready" : text;
    }

    private void OnTabCertificateStatusChanged(object? sender, EventArgs e)
    {
        if (_subscribedTab != null)
        {
            CertificateStatus = _subscribedTab.CertificateStatus;
            CertificateErrorMessage = _subscribedTab.CertificateErrorMessage;
        }
    }

    partial void OnAddressBarTextChanged(string value)
    {
        if (_suppressSuggestions) return;

        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            IsSuggestionsOpen = false;
            Suggestions.Clear();
            return;
        }

        _debounceTimer?.Stop();
        _debounceTimer?.Start();
    }

    private async Task UpdateSuggestionsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            IsSuggestionsOpen = false;
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var historyRepo = scope.ServiceProvider.GetRequiredService<IBrowsingHistoryRepository>();
            var bookmarkRepo = scope.ServiceProvider.GetRequiredService<IBookmarkRepository>();

            var historyTask = historyRepo.SearchWithCountAsync(query, 5);
            var bookmarkTask = bookmarkRepo.GetAllAsync();

            await Task.WhenAll(historyTask, bookmarkTask);

            // Bug 6: Check cancellation before updating UI
            if (cancellationToken.IsCancellationRequested) return;

            var historySuggestions = (await historyTask).Select(h => new AutocompleteSuggestion
            {
                Url = h.Url,
                Title = h.Title ?? h.Url,
                Source = "history",
                VisitCount = h.VisitCount
            });

            string lowerQuery = query.ToLowerInvariant();
            var bookmarkSuggestions = (await bookmarkTask)
                .Where(b => b.Url.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                           b.Title.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .Select(b => new AutocompleteSuggestion
                {
                    Url = b.Url,
                    Title = b.Title,
                    Source = "bookmark",
                    VisitCount = 0
                });

            var merged = historySuggestions
                .Concat(bookmarkSuggestions)
                .GroupBy(s => s.Url)
                .Select(g => g.OrderByDescending(s => s.VisitCount).First())
                .OrderByDescending(s => s.VisitCount)
                .Take(8)
                .ToList();

            // Bug 6: Check cancellation again before mutating the collection
            if (cancellationToken.IsCancellationRequested) return;

            Suggestions.Clear();
            foreach (var s in merged)
            {
                Suggestions.Add(s);
            }

            IsSuggestionsOpen = Suggestions.Count > 0;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Autocomplete error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AcceptSuggestion(AutocompleteSuggestion? suggestion)
    {
        if (suggestion == null) return;

        _suppressSuggestions = true;
        AddressBarText = suggestion.Url;
        _suppressSuggestions = false;
        IsSuggestionsOpen = false;

        var tab = _tabStrip.ActiveTab;
        if (tab != null)
        {
            tab.Navigate(suggestion.Url);
        }
    }

    public void CloseSuggestions()
    {
        IsSuggestionsOpen = false;
    }

    /// <summary>
    /// Navigates to the URL or search query in the address bar.
    /// </summary>
    [RelayCommand]
    private void Navigate()
    {
        if (string.IsNullOrWhiteSpace(AddressBarText)) return;

        var tab = _tabStrip.ActiveTab;
        if (tab == null) return;

        IsSuggestionsOpen = false;
        string url = _searchEngineService.GetNavigationUrl(AddressBarText);
        tab.Navigate(url);
    }

    [RelayCommand]
    private void Back()
    {
        _tabStrip.ActiveTab?.GoBack();
        NotifyNavigationStateChanged();
    }

    [RelayCommand]
    private void Forward()
    {
        _tabStrip.ActiveTab?.GoForward();
        NotifyNavigationStateChanged();
    }

    [RelayCommand]
    private void Refresh()
    {
        _tabStrip.ActiveTab?.Refresh();
    }

    [RelayCommand]
    private void Stop()
    {
        _tabStrip.ActiveTab?.Stop();
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        var tab = _tabStrip.ActiveTab;
        if (tab?.CoreWebView2 != null)
        {
            try
            {
                await tab.CoreWebView2.ExecuteScriptAsync("window.print()");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Print failed: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task HomeAsync()
    {
        var homeUrl = _searchEngineService.GetHomePageUrl();
        var tab = _tabStrip.ActiveTab;
        if (tab != null)
        {
            _suppressSuggestions = true;
            AddressBarText = homeUrl;
            _suppressSuggestions = false;
            tab.Navigate(homeUrl);
        }
        else
        {
            await _tabStrip.NewTabAsync(homeUrl);
        }
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    [RelayCommand]
    private void OpenWorkspace(WorkspaceSection section)
    {
        ActiveWorkspaceSection = section;
        IsWorkspaceOpen = section != WorkspaceSection.None;
    }

    [RelayCommand]
    private void CloseWorkspace()
    {
        ActiveWorkspaceSection = WorkspaceSection.None;
        IsWorkspaceOpen = false;
    }

    /// <summary>
    /// Requests the UI to focus the address bar.
    /// </summary>
    public event EventHandler? FocusAddressBarRequested;

    [RelayCommand]
    private void FocusAddressBar()
    {
        FocusAddressBarRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ZoomIn()
    {
        var tab = _tabStrip.ActiveTab;
        if (tab != null)
        {
            var newZoom = Math.Min(tab.ZoomLevel + 0.1, 3.0);
            tab.SetZoom(newZoom);
            StatusBarText = $"Zoom: {newZoom:P0}";
        }
    }

    [RelayCommand]
    private void ZoomOut()
    {
        var tab = _tabStrip.ActiveTab;
        if (tab != null)
        {
            var newZoom = Math.Max(tab.ZoomLevel - 0.1, 0.25);
            tab.SetZoom(newZoom);
            StatusBarText = $"Zoom: {newZoom:P0}";
        }
    }

    [RelayCommand]
    private void ZoomReset()
    {
        var tab = _tabStrip.ActiveTab;
        if (tab != null)
        {
            tab.SetZoom(1.0);
            StatusBarText = "Zoom: 100%";
        }
    }

    /// <summary>
    /// Requests the UI to toggle full-screen mode.
    /// </summary>
    public event EventHandler? ToggleFullScreenRequested;

    [RelayCommand]
    private void ToggleFullScreen()
    {
        ToggleFullScreenRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Find in page (delegates to WebView2 built-in find).
    /// </summary>
    public event EventHandler? FindInPageRequested;

    [RelayCommand]
    private void FindInPage()
    {
        FindInPageRequested?.Invoke(this, EventArgs.Empty);
    }

    private void NotifyNavigationStateChanged()
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Bug 10: Properly unsubscribe and dispose debounce timer
        if (_debounceTimer != null)
        {
            _debounceTimer.Stop();
            _debounceTimer.Tick -= OnDebounceTimerTick;
            _debounceTimer = null;
        }

        // Bug 6: Dispose cancellation token source
        _autocompleteCts?.Cancel();
        _autocompleteCts?.Dispose();
        _autocompleteCts = null;

        _tabStrip.ActiveTabChanged -= OnActiveTabChanged;

        if (_settingsService != null)
        {
            _settingsService.ShowBookmarksBarChanged -= OnShowBookmarksBarChanged;
        }

        if (_extensionService != null)
        {
            _extensionService.AdBlockerStateChanged -= OnAdBlockerStateChanged;
        }

        if (_subscribedTab != null)
        {
            _subscribedTab.SourceChanged -= OnTabSourceChanged;
            _subscribedTab.NavigationStarting -= OnTabNavigationStarting;
            _subscribedTab.NavigationCompleted -= OnTabNavigationCompleted;
            _subscribedTab.TitleChanged -= OnTabTitleChanged;
            _subscribedTab.StatusBarTextChanged -= OnTabStatusBarTextChanged;
            _subscribedTab.CertificateStatusChanged -= OnTabCertificateStatusChanged;
        }

        GC.SuppressFinalize(this);
    }
}
