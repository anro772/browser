using BrowserApp.Core.Models;

namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Service for handling browser navigation operations.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Fired when navigation starts.
    /// </summary>
    event EventHandler<NavigationEventArgs>? NavigationStarting;

    /// <summary>
    /// Fired when navigation completes.
    /// </summary>
    event EventHandler<NavigationEventArgs>? NavigationCompleted;

    /// <summary>
    /// Fired when the source URL changes.
    /// </summary>
    event EventHandler<string>? SourceChanged;

    /// <summary>
    /// Initializes the navigation service.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    Task NavigateAsync(string url);

    /// <summary>
    /// Navigates back in history.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Navigates forward in history.
    /// </summary>
    void GoForward();

    /// <summary>
    /// Refreshes the current page.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Gets whether back navigation is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets whether forward navigation is possible.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Gets the current URL.
    /// </summary>
    string CurrentUrl { get; }
}
