using BrowserApp.Core.Models;

namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Interface for intercepting and capturing network requests from WebView2.
/// </summary>
public interface IRequestInterceptor
{
    /// <summary>
    /// Raised when a network request is captured.
    /// </summary>
    event EventHandler<NetworkRequest>? RequestCaptured;

    /// <summary>
    /// Initializes the request interceptor.
    /// Must be called after WebView2 is ready.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Enables request interception.
    /// </summary>
    void Enable();

    /// <summary>
    /// Disables request interception.
    /// </summary>
    void Disable();

    /// <summary>
    /// Gets whether request interception is currently enabled.
    /// </summary>
    bool IsEnabled { get; }
}
