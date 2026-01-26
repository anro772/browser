using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the main browser window.
/// Handles address bar input and navigation commands.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly INavigationService _navigationService;
    private readonly ISearchEngineService _searchEngineService;
    private readonly IBrowsingHistoryRepository _historyRepository;
    private bool _isDisposed;

    [ObservableProperty]
    private string _addressBarText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _pageTitle = "AI-Powered Privacy Browser";

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    public bool CanGoBack => _navigationService.CanGoBack;
    public bool CanGoForward => _navigationService.CanGoForward;

    public MainViewModel(
        INavigationService navigationService,
        ISearchEngineService searchEngineService,
        IBrowsingHistoryRepository historyRepository)
    {
        _navigationService = navigationService;
        _searchEngineService = searchEngineService;
        _historyRepository = historyRepository;

        // Subscribe to navigation events
        _navigationService.SourceChanged += OnSourceChanged;
        _navigationService.NavigationStarting += OnNavigationStarting;
        _navigationService.NavigationCompleted += OnNavigationCompleted;
    }

    /// <summary>
    /// Navigates to the URL or search query in the address bar.
    /// </summary>
    [RelayCommand]
    private async Task NavigateAsync()
    {
        if (string.IsNullOrWhiteSpace(AddressBarText))
        {
            return;
        }

        string url = _searchEngineService.GetNavigationUrl(AddressBarText);
        await _navigationService.NavigateAsync(url);
    }

    /// <summary>
    /// Navigates back in history.
    /// </summary>
    [RelayCommand]
    private void Back()
    {
        _navigationService.GoBack();
        NotifyNavigationStateChanged();
    }

    /// <summary>
    /// Navigates forward in history.
    /// </summary>
    [RelayCommand]
    private void Forward()
    {
        _navigationService.GoForward();
        NotifyNavigationStateChanged();
    }

    /// <summary>
    /// Refreshes the current page.
    /// </summary>
    [RelayCommand]
    private void Refresh()
    {
        _navigationService.Refresh();
    }

    /// <summary>
    /// Navigates to the home page (Google).
    /// </summary>
    [RelayCommand]
    private async Task HomeAsync()
    {
        AddressBarText = "https://www.google.com";
        await _navigationService.NavigateAsync("https://www.google.com");
    }

    /// <summary>
    /// Toggles the sidebar visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    private void OnSourceChanged(object? sender, string newUrl)
    {
        AddressBarText = newUrl;
        NotifyNavigationStateChanged();
    }

    private void OnNavigationStarting(object? sender, NavigationEventArgs e)
    {
        IsLoading = true;
    }

    private async void OnNavigationCompleted(object? sender, NavigationEventArgs e)
    {
        IsLoading = false;
        NotifyNavigationStateChanged();

        // Record browsing history for successful navigations
        if (e.IsSuccess && !string.IsNullOrEmpty(e.Url))
        {
            try
            {
                await _historyRepository.AddAsync(new BrowsingHistoryEntity
                {
                    Url = e.Url,
                    Title = e.Title,
                    VisitedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to record history: {ex.Message}");
            }
        }
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

        // Unsubscribe from navigation events to prevent memory leak
        _navigationService.SourceChanged -= OnSourceChanged;
        _navigationService.NavigationStarting -= OnNavigationStarting;
        _navigationService.NavigationCompleted -= OnNavigationCompleted;

        GC.SuppressFinalize(this);
    }
}
