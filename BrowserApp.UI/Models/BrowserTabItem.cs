using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using BrowserApp.Core.Interfaces;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.Models;

/// <summary>
/// Represents a single browser tab with its own WebView2 instance.
/// Each tab is fully independent with its own navigation, request interception, and content injection.
/// </summary>
public partial class BrowserTabItem : ObservableObject, IDisposable
{
    private WebView2? _webView;
    private CoreWebView2? _coreWebView2;
    private RequestInterceptor? _requestInterceptor;
    private CSSInjector? _cssInjector;
    private JSInjector? _jsInjector;
    private IRuleEngine? _ruleEngine;
    private bool _isDisposed;
    private bool _isCoreInitialized;

    public Guid Id { get; } = Guid.NewGuid();

    [ObservableProperty]
    private string _title = "New Tab";

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ImageSource? _favicon;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    public WebView2? WebView => _webView;
    public CoreWebView2? CoreWebView2 => _coreWebView2;
    public RequestInterceptor? RequestInterceptor => _requestInterceptor;

    public bool CanGoBack => _coreWebView2?.CanGoBack ?? false;
    public bool CanGoForward => _coreWebView2?.CanGoForward ?? false;

    /// <summary>
    /// Fired when the source URL changes in this tab.
    /// </summary>
    public event EventHandler<string>? SourceChanged;

    /// <summary>
    /// Fired when navigation starts in this tab.
    /// </summary>
    public event EventHandler? NavigationStarting;

    /// <summary>
    /// Fired when navigation completes in this tab.
    /// </summary>
    public event EventHandler<bool>? NavigationCompleted;

    /// <summary>
    /// Fired when the document title changes.
    /// </summary>
    public event EventHandler<string>? TitleChanged;

    /// <summary>
    /// Fired when StatusBar text changes (link hover).
    /// </summary>
    public event EventHandler<string>? StatusBarTextChanged;

    /// <summary>
    /// Phase 1: Creates the WebView2 control (must be added to visual tree before Phase 2).
    /// </summary>
    public void CreateWebView()
    {
        _webView = new WebView2();
    }

    /// <summary>
    /// Phase 2: Initializes CoreWebView2 after the WebView2 is in the visual tree.
    /// The WebView2 must have an HWND (be parented in the visual tree) before this call.
    /// </summary>
    public async Task InitializeCoreAsync(
        CoreWebView2Environment environment,
        IBlockingService blockingService,
        IRuleEngine ruleEngine)
    {
        if (_webView == null || _isCoreInitialized) return;

        await _webView.EnsureCoreWebView2Async(environment);
        _coreWebView2 = _webView.CoreWebView2;
        _isCoreInitialized = true;

        ConfigureSettings();
        SubscribeToEvents();

        // Create per-tab interceptor, CSS injector, JS injector
        _requestInterceptor = new RequestInterceptor(blockingService);
        _requestInterceptor.SetCoreWebView2(_coreWebView2);
        await _requestInterceptor.InitializeAsync();

        _cssInjector = new CSSInjector();
        _cssInjector.SetCoreWebView2(_coreWebView2);

        _jsInjector = new JSInjector();
        _jsInjector.SetCoreWebView2(_coreWebView2);

        // Store rule engine for injection handler
        _ruleEngine = ruleEngine;

        // Wire navigation completed to execute injections (named method for proper cleanup)
        _coreWebView2.NavigationCompleted += OnNavigationCompletedForInjection;
    }

    private async void OnNavigationCompletedForInjection(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess && _ruleEngine != null)
        {
            await ExecuteInjectionsAsync(_ruleEngine);
        }
    }

    private void ConfigureSettings()
    {
        if (_coreWebView2 == null) return;

        _coreWebView2.Settings.IsPasswordAutosaveEnabled = true;
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
        _coreWebView2.DocumentTitleChanged += OnDocumentTitleChanged;
        _coreWebView2.StatusBarTextChanged += OnStatusBarTextChanged;
        _coreWebView2.FaviconChanged += OnFaviconChanged;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        IsLoading = true;
        NavigationStarting?.Invoke(this, EventArgs.Empty);
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        IsLoading = false;
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
        NavigationCompleted?.Invoke(this, e.IsSuccess);
    }

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        var currentUrl = _coreWebView2?.Source ?? string.Empty;
        Url = currentUrl;
        SourceChanged?.Invoke(this, currentUrl);
    }

    private void OnDocumentTitleChanged(object? sender, object e)
    {
        var title = _coreWebView2?.DocumentTitle ?? "New Tab";
        Title = title;
        TitleChanged?.Invoke(this, title);
    }

    private void OnStatusBarTextChanged(object? sender, object e)
    {
        var text = _coreWebView2?.StatusBarText ?? string.Empty;
        StatusBarTextChanged?.Invoke(this, text);
    }

    private async void OnFaviconChanged(object? sender, object e)
    {
        if (_coreWebView2 == null) return;

        try
        {
            using var stream = await _coreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
            if (stream != null && stream.Length > 0)
            {
                stream.Position = 0;
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.DecodePixelWidth = 16;
                bitmap.DecodePixelHeight = 16;
                bitmap.EndInit();
                bitmap.Freeze();

                Application.Current?.Dispatcher.Invoke(() => Favicon = bitmap);
            }
        }
        catch
        {
            // Favicon loading is best-effort
        }
    }

    private async Task ExecuteInjectionsAsync(IRuleEngine ruleEngine)
    {
        try
        {
            var currentUrl = _coreWebView2?.Source;
            if (string.IsNullOrEmpty(currentUrl)) return;

            var injections = ruleEngine.GetInjectionsForPage(currentUrl);

            foreach (var injection in injections)
            {
                if (injection.Type == "inject_css" && _cssInjector != null && !string.IsNullOrEmpty(injection.Css))
                {
                    await _cssInjector.InjectAsync(injection.Css, injection.Timing);
                }
                else if (injection.Type == "inject_js" && _jsInjector != null && !string.IsNullOrEmpty(injection.Js))
                {
                    await _jsInjector.InjectAsync(injection.Js, injection.Timing);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BrowserTabItem] Injection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Navigate this tab to a URL.
    /// </summary>
    public void Navigate(string url)
    {
        _coreWebView2?.Navigate(url);
    }

    public void GoBack()
    {
        if (CanGoBack) _coreWebView2?.GoBack();
    }

    public void GoForward()
    {
        if (CanGoForward) _coreWebView2?.GoForward();
    }

    public void Refresh()
    {
        _coreWebView2?.Reload();
    }

    public void SetZoom(double zoomFactor)
    {
        if (_webView != null)
        {
            _webView.ZoomFactor = zoomFactor;
            ZoomLevel = zoomFactor;
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_coreWebView2 != null)
        {
            _coreWebView2.NavigationStarting -= OnNavigationStarting;
            _coreWebView2.NavigationCompleted -= OnNavigationCompleted;
            _coreWebView2.NavigationCompleted -= OnNavigationCompletedForInjection;
            _coreWebView2.SourceChanged -= OnSourceChanged;
            _coreWebView2.DocumentTitleChanged -= OnDocumentTitleChanged;
            _coreWebView2.StatusBarTextChanged -= OnStatusBarTextChanged;
            _coreWebView2.FaviconChanged -= OnFaviconChanged;
        }

        _cssInjector?.Dispose();
        _jsInjector?.Dispose();

        _webView?.Dispose();
        _webView = null;
        _coreWebView2 = null;

        GC.SuppressFinalize(this);
    }
}
