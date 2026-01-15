using System.IO;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for handling browser navigation operations.
/// Wraps WebView2 navigation functionality.
/// </summary>
public class NavigationService : INavigationService
{
    private WebView2? _webView;
    private CoreWebView2? _coreWebView2;
    private bool _isInitialized;

    public event EventHandler<NavigationEventArgs>? NavigationStarting;
    public event EventHandler<NavigationEventArgs>? NavigationCompleted;
    public event EventHandler<string>? SourceChanged;

    public bool CanGoBack => _coreWebView2?.CanGoBack ?? false;
    public bool CanGoForward => _coreWebView2?.CanGoForward ?? false;
    public string CurrentUrl => _coreWebView2?.Source ?? string.Empty;

    /// <summary>
    /// Sets the WebView2 control to use for navigation.
    /// Must be called before InitializeAsync.
    /// </summary>
    public void SetWebView(WebView2 webView)
    {
        _webView = webView;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        if (_isInitialized || _webView == null)
        {
            return;
        }

        string userDataFolder = GetUserDataFolder();

        // Create environment with custom user data folder
        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder
        );

        await _webView.EnsureCoreWebView2Async(environment);
        _coreWebView2 = _webView.CoreWebView2;

        // Configure settings
        ConfigureSettings();

        // Subscribe to events
        SubscribeToEvents();

        _isInitialized = true;
    }

    /// <inheritdoc/>
    public async Task NavigateAsync(string url)
    {
        if (_coreWebView2 == null)
        {
            await InitializeAsync();
        }

        _coreWebView2?.Navigate(url);
    }

    /// <inheritdoc/>
    public void GoBack()
    {
        if (CanGoBack)
        {
            _coreWebView2?.GoBack();
        }
    }

    /// <inheritdoc/>
    public void GoForward()
    {
        if (CanGoForward)
        {
            _coreWebView2?.GoForward();
        }
    }

    /// <inheritdoc/>
    public void Refresh()
    {
        _coreWebView2?.Reload();
    }

    private void ConfigureSettings()
    {
        if (_coreWebView2 == null) return;

        // Enable password manager (autosave passwords when user logs in)
        _coreWebView2.Settings.IsPasswordAutosaveEnabled = true;

        // Enable other useful features
        _coreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        _coreWebView2.Settings.AreDevToolsEnabled = true;
        _coreWebView2.Settings.IsStatusBarEnabled = true;
        _coreWebView2.Settings.IsZoomControlEnabled = true;
    }

    private void SubscribeToEvents()
    {
        if (_coreWebView2 == null) return;

        _coreWebView2.NavigationStarting += OnNavigationStarting;
        _coreWebView2.NavigationCompleted += OnNavigationCompleted;
        _coreWebView2.SourceChanged += OnSourceChanged;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        NavigationStarting?.Invoke(this, new NavigationEventArgs
        {
            Url = e.Uri,
            IsSuccess = false
        });
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        NavigationCompleted?.Invoke(this, new NavigationEventArgs
        {
            Url = CurrentUrl,
            IsSuccess = e.IsSuccess,
            HttpStatusCode = e.HttpStatusCode
        });
    }

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        SourceChanged?.Invoke(this, CurrentUrl);
    }

    private static string GetUserDataFolder()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string userDataPath = Path.Combine(appDataPath, "BrowserApp", "UserData");

        if (!Directory.Exists(userDataPath))
        {
            Directory.CreateDirectory(userDataPath);
        }

        return userDataPath;
    }
}
