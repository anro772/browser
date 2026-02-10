using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Core.Interfaces;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Models;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the main browser window.
/// Delegates navigation to the active tab via TabStripViewModel.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ISearchEngineService _searchEngineService;
    private readonly IBrowsingHistoryRepository _historyRepository;
    private readonly TabStripViewModel _tabStrip;
    private readonly BookmarkViewModel _bookmarkViewModel;
    private bool _isDisposed;

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

    public TabStripViewModel TabStrip => _tabStrip;
    public BookmarkViewModel BookmarkViewModel => _bookmarkViewModel;

    public bool CanGoBack => _tabStrip.ActiveTab?.CanGoBack ?? false;
    public bool CanGoForward => _tabStrip.ActiveTab?.CanGoForward ?? false;

    public MainViewModel(
        ISearchEngineService searchEngineService,
        IBrowsingHistoryRepository historyRepository,
        TabStripViewModel tabStrip,
        BookmarkViewModel bookmarkViewModel)
    {
        _searchEngineService = searchEngineService;
        _historyRepository = historyRepository;
        _tabStrip = tabStrip;
        _bookmarkViewModel = bookmarkViewModel;

        // Subscribe to active tab changes
        _tabStrip.ActiveTabChanged += OnActiveTabChanged;
    }

    private BrowserTabItem? _subscribedTab;

    private void OnActiveTabChanged(object? sender, BrowserTabItem? tab)
    {
        // Unsubscribe from previous tab
        if (_subscribedTab != null)
        {
            _subscribedTab.SourceChanged -= OnTabSourceChanged;
            _subscribedTab.NavigationStarting -= OnTabNavigationStarting;
            _subscribedTab.NavigationCompleted -= OnTabNavigationCompleted;
            _subscribedTab.TitleChanged -= OnTabTitleChanged;
            _subscribedTab.StatusBarTextChanged -= OnTabStatusBarTextChanged;
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

            // Update UI to match active tab state
            AddressBarText = tab.Url;
            PageTitle = string.IsNullOrEmpty(tab.Title) || tab.Title == "New Tab"
                ? "Privacy Browser"
                : $"{tab.Title} - Privacy Browser";
            IsLoading = tab.IsLoading;
        }

        NotifyNavigationStateChanged();
    }

    private void OnTabSourceChanged(object? sender, string newUrl)
    {
        AddressBarText = newUrl;
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

    /// <summary>
    /// Navigates to the URL or search query in the address bar.
    /// </summary>
    [RelayCommand]
    private void Navigate()
    {
        if (string.IsNullOrWhiteSpace(AddressBarText)) return;

        var tab = _tabStrip.ActiveTab;
        if (tab == null) return;

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
    private async Task HomeAsync()
    {
        var tab = _tabStrip.ActiveTab;
        if (tab != null)
        {
            AddressBarText = "https://www.google.com";
            tab.Navigate("https://www.google.com");
        }
        else
        {
            await _tabStrip.NewTabAsync("https://www.google.com");
        }
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
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

        _tabStrip.ActiveTabChanged -= OnActiveTabChanged;

        if (_subscribedTab != null)
        {
            _subscribedTab.SourceChanged -= OnTabSourceChanged;
            _subscribedTab.NavigationStarting -= OnTabNavigationStarting;
            _subscribedTab.NavigationCompleted -= OnTabNavigationCompleted;
            _subscribedTab.TitleChanged -= OnTabTitleChanged;
            _subscribedTab.StatusBarTextChanged -= OnTabStatusBarTextChanged;
        }

        GC.SuppressFinalize(this);
    }
}
