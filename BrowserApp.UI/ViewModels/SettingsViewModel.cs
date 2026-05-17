using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;
using BrowserApp.UI.Views;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the settings dialog.
/// Manages user preferences including privacy mode, search engine, and server configuration.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISearchEngineService _searchEngineService;
    private readonly ExtensionService _extensionService;

    [ObservableProperty]
    private bool _isAdBlockerEnabled;

    [ObservableProperty]
    private PrivacyMode _selectedPrivacyMode;

    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private string _selectedSearchEngine = "Google";

    [ObservableProperty]
    private string _customSearchEngineUrl = string.Empty;

    [ObservableProperty]
    private bool _isCustomEngineVisible;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _userTagDisplay = string.Empty;

    [ObservableProperty]
    private string _homePage = string.Empty;

    [ObservableProperty]
    private string _defaultDownloadPath = string.Empty;

    [ObservableProperty]
    private StartupBehavior _selectedStartupBehavior = StartupBehavior.RestoreSession;

    [ObservableProperty]
    private bool _showBookmarksBar;

    /// <summary>
    /// Counter incremented every time auto-save fires. View binds a Storyboard's
    /// "play" trigger to changes here (via SavedPillAnimator) so every save shows
    /// a quick "Saved ✓" pill.
    /// </summary>
    [ObservableProperty]
    private int _lastSavedCounter;

    [ObservableProperty]
    private string _homePageError = string.Empty;

    [ObservableProperty]
    private string _serverUrlError = string.Empty;

    [ObservableProperty]
    private string _customSearchEngineError = string.Empty;

    /// <summary>
    /// Available privacy modes for the dropdown.
    /// </summary>
    public IReadOnlyList<PrivacyModeOption> PrivacyModes { get; } = new List<PrivacyModeOption>
    {
        new(PrivacyMode.Relaxed, "Relaxed", "Minimal blocking - best for sites that break with aggressive blocking"),
        new(PrivacyMode.Standard, "Standard", "Balanced blocking - recommended for daily browsing"),
        new(PrivacyMode.Strict, "Strict", "Maximum blocking - may break some site functionality")
    };

    public IReadOnlyList<string> SearchEngines { get; }

    public IReadOnlyList<StartupBehaviorOption> StartupBehaviors { get; } = new List<StartupBehaviorOption>
    {
        new(StartupBehavior.RestoreSession, "Restore previous session", "Reopen all tabs from your last browsing session"),
        new(StartupBehavior.NewTab, "Open new tab", "Start with a blank new tab page"),
        new(StartupBehavior.HomePage, "Open home page", "Navigate to your configured home page")
    };

    public SettingsViewModel(
        SettingsService settingsService,
        IServiceScopeFactory scopeFactory,
        ISearchEngineService searchEngineService,
        ExtensionService extensionService)
    {
        _settingsService = settingsService;
        _scopeFactory = scopeFactory;
        _searchEngineService = searchEngineService;
        _extensionService = extensionService;

        SearchEngines = searchEngineService.AvailableEngines;

        // Load current settings
        SelectedPrivacyMode = _settingsService.PrivacyMode;
        ServerUrl = _settingsService.ServerUrl;
        SelectedSearchEngine = _settingsService.SearchEngine;
        CustomSearchEngineUrl = _settingsService.CustomSearchEngineUrl;
        IsCustomEngineVisible = SelectedSearchEngine == "Custom";
        HomePage = _settingsService.HomePage;
        DefaultDownloadPath = _settingsService.DefaultDownloadPath;
        SelectedStartupBehavior = _settingsService.StartupBehavior;
        ShowBookmarksBar = _settingsService.ShowBookmarksBar;

        // Load username and tag
        Username = _settingsService.Username;
        UserTagDisplay = $"#{_settingsService.UserTag}";

        // Load ad blocker state asynchronously
        _ = LoadAdBlockerStateAsync();
    }

    private async Task LoadAdBlockerStateAsync()
    {
        try
        {
            IsAdBlockerEnabled = await _extensionService.IsAdBlockerEnabledAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("[Settings] Failed to load ad blocker state", ex);
        }
    }

    /// <summary>
    /// Re-reads the ad-blocker state from the DB. Settings panel calls this on Loaded,
    /// because the VM is constructed eagerly at app startup (before the first tab opens),
    /// while the built-in extension is registered only after the first tab triggers
    /// EnsureBuiltInExtensionsAsync. Without this refresh, the toggle would show stale
    /// "off" even when the DB and WebView2 say "on".
    /// </summary>
    public async Task RefreshAdBlockerStateAsync()
    {
        await LoadAdBlockerStateAsync();
    }

    async partial void OnIsAdBlockerEnabledChanged(bool value)
    {
        try
        {
            await _extensionService.SetAdBlockerEnabledAsync(value);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("[Settings] Failed to toggle ad blocker", ex);
        }
    }

    /// <summary>
    /// Clears all browsing history.
    /// </summary>
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        if (!ConfirmDialog.Show(Application.Current.MainWindow,
                "Clear browsing history",
                "Are you sure you want to clear all browsing history? This cannot be undone.",
                destructive: true,
                confirmText: "Clear"))
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var historyRepository = scope.ServiceProvider.GetRequiredService<IBrowsingHistoryRepository>();
            await historyRepository.ClearAllAsync();
            BumpSaved();
        }
        catch (Exception ex)
        {
            ConfirmDialog.Show(Application.Current.MainWindow,
                "Clear failed",
                $"Failed to clear history: {ex.Message}",
                showCancel: false);
        }
    }

    /// <summary>
    /// Clears all network logs.
    /// </summary>
    [RelayCommand]
    private async Task ClearNetworkLogsAsync()
    {
        if (!ConfirmDialog.Show(Application.Current.MainWindow,
                "Clear network logs",
                "Are you sure you want to clear all network logs? This cannot be undone.",
                destructive: true,
                confirmText: "Clear"))
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var networkLogRepository = scope.ServiceProvider.GetRequiredService<INetworkLogRepository>();
            await networkLogRepository.ClearAllAsync();
            BumpSaved();
        }
        catch (Exception ex)
        {
            ConfirmDialog.Show(Application.Current.MainWindow,
                "Clear failed",
                $"Failed to clear network logs: {ex.Message}",
                showCancel: false);
        }
    }

    private void BumpSaved() => LastSavedCounter++;

    private static bool IsValidUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;  // empty = use default
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    partial void OnSelectedPrivacyModeChanged(PrivacyMode value)
    {
        _settingsService.PrivacyMode = value;
        BumpSaved();
    }

    partial void OnServerUrlChanged(string value)
    {
        if (!IsValidUrl(value))
        {
            ServerUrlError = "Enter a valid http(s) URL.";
            return;
        }
        ServerUrlError = string.Empty;
        _settingsService.ServerUrl = value;
        BumpSaved();
    }

    partial void OnSelectedSearchEngineChanged(string value)
    {
        IsCustomEngineVisible = value == "Custom";
        _settingsService.SearchEngine = value;

        if (value == "Custom" && !string.IsNullOrWhiteSpace(CustomSearchEngineUrl))
        {
            _searchEngineService.SetCustomSearchEngine(CustomSearchEngineUrl);
        }
        else
        {
            _searchEngineService.SetSearchEngine(value);
        }
        BumpSaved();
    }

    partial void OnCustomSearchEngineUrlChanged(string value)
    {
        // Custom engine URL must contain a {query} placeholder somewhere; basic shape check first.
        if (!string.IsNullOrWhiteSpace(value)
            && !Uri.TryCreate(value.Replace("{query}", "test"), UriKind.Absolute, out _))
        {
            CustomSearchEngineError = "Enter a full URL with {query} placeholder.";
            return;
        }
        CustomSearchEngineError = string.Empty;
        _settingsService.CustomSearchEngineUrl = value;
        if (SelectedSearchEngine == "Custom" && !string.IsNullOrWhiteSpace(value))
        {
            _searchEngineService.SetCustomSearchEngine(value);
        }
        BumpSaved();
    }

    partial void OnHomePageChanged(string value)
    {
        if (!IsValidUrl(value))
        {
            HomePageError = "Enter a valid http(s) URL.";
            return;
        }
        HomePageError = string.Empty;
        _settingsService.HomePage = value;
        BumpSaved();
    }

    partial void OnDefaultDownloadPathChanged(string value)
    {
        _settingsService.DefaultDownloadPath = value;
        BumpSaved();
    }

    partial void OnSelectedStartupBehaviorChanged(StartupBehavior value)
    {
        _settingsService.StartupBehavior = value;
        BumpSaved();
    }

    partial void OnShowBookmarksBarChanged(bool value)
    {
        _settingsService.ShowBookmarksBar = value;
        BumpSaved();
    }

    partial void OnUsernameChanged(string value)
    {
        _settingsService.Username = value;
        BumpSaved();
    }
}

/// <summary>
/// Represents a privacy mode option for display in the UI.
/// </summary>
public record PrivacyModeOption(PrivacyMode Mode, string Name, string Description);

/// <summary>
/// Represents a startup behavior option for display in the UI.
/// </summary>
public record StartupBehaviorOption(StartupBehavior Behavior, string Name, string Description);
