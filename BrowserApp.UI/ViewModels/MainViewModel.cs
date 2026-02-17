using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TabStripViewModel _tabStrip;
    private readonly BookmarkViewModel _bookmarkViewModel;
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

    public TabStripViewModel TabStrip => _tabStrip;
    public BookmarkViewModel BookmarkViewModel => _bookmarkViewModel;

    public bool CanGoBack => _tabStrip.ActiveTab?.CanGoBack ?? false;
    public bool CanGoForward => _tabStrip.ActiveTab?.CanGoForward ?? false;

    public MainViewModel(
        ISearchEngineService searchEngineService,
        IBrowsingHistoryRepository historyRepository,
        IServiceScopeFactory scopeFactory,
        TabStripViewModel tabStrip,
        BookmarkViewModel bookmarkViewModel)
    {
        _searchEngineService = searchEngineService;
        _historyRepository = historyRepository;
        _scopeFactory = scopeFactory;
        _tabStrip = tabStrip;
        _bookmarkViewModel = bookmarkViewModel;

        // Subscribe to active tab changes
        _tabStrip.ActiveTabChanged += OnActiveTabChanged;

        // Setup debounce timer for autocomplete (Bug 10: named handler for proper cleanup)
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _debounceTimer.Tick += OnDebounceTimerTick;
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
