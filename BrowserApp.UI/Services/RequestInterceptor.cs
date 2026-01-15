using Microsoft.Web.WebView2.Core;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;

namespace BrowserApp.UI.Services;

/// <summary>
/// Intercepts network requests from WebView2 using WebResourceRequested event.
/// Captures all HTTP requests for monitoring and potential blocking.
/// </summary>
public class RequestInterceptor : IRequestInterceptor
{
    private CoreWebView2? _coreWebView2;
    private readonly IBlockingService? _blockingService;
    private bool _isEnabled;
    private bool _isInitialized;

    public event EventHandler<NetworkRequest>? RequestCaptured;

    public bool IsEnabled => _isEnabled;

    public RequestInterceptor()
    {
    }

    public RequestInterceptor(IBlockingService blockingService)
    {
        _blockingService = blockingService;
    }

    /// <summary>
    /// Sets the CoreWebView2 instance to intercept requests from.
    /// Must be called before InitializeAsync.
    /// </summary>
    public void SetCoreWebView2(CoreWebView2 coreWebView2)
    {
        _coreWebView2 = coreWebView2;
    }

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        if (_isInitialized || _coreWebView2 == null)
        {
            return Task.CompletedTask;
        }

        // Add filter to capture all resource types
        _coreWebView2.AddWebResourceRequestedFilter(
            "*",
            CoreWebView2WebResourceContext.All);

        // Subscribe to request events
        _coreWebView2.WebResourceRequested += OnWebResourceRequested;
        _coreWebView2.WebResourceResponseReceived += OnWebResourceResponseReceived;

        _isInitialized = true;
        _isEnabled = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Enable()
    {
        _isEnabled = true;
    }

    /// <inheritdoc/>
    public void Disable()
    {
        _isEnabled = false;
    }

    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (!_isEnabled) return;

        try
        {
            var request = new NetworkRequest
            {
                Url = e.Request.Uri,
                Method = e.Request.Method,
                ResourceType = MapResourceContext(e.ResourceContext),
                Timestamp = DateTime.UtcNow,
                WasBlocked = false
            };

            // Check if request should be blocked
            if (_blockingService != null)
            {
                var currentPageUrl = _coreWebView2?.Source;
                var evaluation = _blockingService.ShouldBlockRequest(request, currentPageUrl);

                if (evaluation.ShouldBlock)
                {
                    // Block the request by setting a 403 response
                    var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("Blocked by BrowserApp rule"));
                    e.Response = _coreWebView2!.Environment.CreateWebResourceResponse(
                        stream, 403, "Blocked", "Content-Type: text/plain");

                    // Create a new request object with blocking info
                    request = new NetworkRequest
                    {
                        Url = request.Url,
                        Method = request.Method,
                        ResourceType = request.ResourceType,
                        Timestamp = request.Timestamp,
                        WasBlocked = true,
                        BlockedByRuleId = evaluation.BlockedByRuleId
                    };

                    System.Diagnostics.Debug.WriteLine($"[RequestInterceptor] Blocked: {request.Url}");
                }
            }

            RequestCaptured?.Invoke(this, request);
        }
        catch (Exception ex)
        {
            // Log error but don't crash the browser
            System.Diagnostics.Debug.WriteLine($"RequestInterceptor error: {ex.Message}");
        }
    }

    private void OnWebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        if (!_isEnabled) return;

        try
        {
            // Get Content-Length header for size
            long? size = null;
            string? contentType = null;

            var headers = e.Response.Headers;

            // Try to get content length
            if (headers.Contains("Content-Length"))
            {
                var contentLength = headers.GetHeader("Content-Length");
                if (long.TryParse(contentLength, out var length))
                {
                    size = length;
                }
            }

            // Get content type
            if (headers.Contains("Content-Type"))
            {
                contentType = headers.GetHeader("Content-Type");
            }

            // Create request with response info
            var request = new NetworkRequest
            {
                Url = e.Request.Uri,
                Method = e.Request.Method,
                StatusCode = e.Response.StatusCode,
                ContentType = contentType,
                Size = size,
                ResourceType = MapResourceContext(e.Request),
                Timestamp = DateTime.UtcNow,
                WasBlocked = false
            };

            RequestCaptured?.Invoke(this, request);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RequestInterceptor response error: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps WebView2 resource context to a readable string.
    /// </summary>
    private static string MapResourceContext(CoreWebView2WebResourceContext context)
    {
        return context switch
        {
            CoreWebView2WebResourceContext.Document => "Document",
            CoreWebView2WebResourceContext.Stylesheet => "Stylesheet",
            CoreWebView2WebResourceContext.Image => "Image",
            CoreWebView2WebResourceContext.Media => "Media",
            CoreWebView2WebResourceContext.Font => "Font",
            CoreWebView2WebResourceContext.Script => "Script",
            CoreWebView2WebResourceContext.XmlHttpRequest => "XHR",
            CoreWebView2WebResourceContext.Fetch => "Fetch",
            CoreWebView2WebResourceContext.TextTrack => "TextTrack",
            CoreWebView2WebResourceContext.EventSource => "EventSource",
            CoreWebView2WebResourceContext.Websocket => "WebSocket",
            CoreWebView2WebResourceContext.Manifest => "Manifest",
            CoreWebView2WebResourceContext.SignedExchange => "SignedExchange",
            CoreWebView2WebResourceContext.Ping => "Ping",
            CoreWebView2WebResourceContext.CspViolationReport => "CSP",
            CoreWebView2WebResourceContext.Other => "Other",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Maps resource context from request (for response handler).
    /// </summary>
    private static string MapResourceContext(CoreWebView2WebResourceRequestedEventArgs e)
    {
        return MapResourceContext(e.ResourceContext);
    }

    /// <summary>
    /// Infers resource type from request when context isn't available.
    /// </summary>
    private static string MapResourceContext(CoreWebView2WebResourceRequest request)
    {
        // Infer from URL extension
        var url = request.Uri.ToLowerInvariant();

        if (url.EndsWith(".js")) return "Script";
        if (url.EndsWith(".css")) return "Stylesheet";
        if (url.EndsWith(".html") || url.EndsWith(".htm")) return "Document";
        if (url.EndsWith(".json")) return "Fetch";
        if (url.EndsWith(".png") || url.EndsWith(".jpg") ||
            url.EndsWith(".jpeg") || url.EndsWith(".gif") ||
            url.EndsWith(".webp") || url.EndsWith(".svg") ||
            url.EndsWith(".ico")) return "Image";
        if (url.EndsWith(".woff") || url.EndsWith(".woff2") ||
            url.EndsWith(".ttf") || url.EndsWith(".otf")) return "Font";
        if (url.EndsWith(".mp4") || url.EndsWith(".webm") ||
            url.EndsWith(".mp3") || url.EndsWith(".ogg")) return "Media";

        return "Other";
    }
}
