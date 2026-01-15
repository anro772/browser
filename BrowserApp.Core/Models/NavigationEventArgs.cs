namespace BrowserApp.Core.Models;

/// <summary>
/// Event arguments for navigation events.
/// </summary>
public class NavigationEventArgs : EventArgs
{
    /// <summary>
    /// The URL being navigated to or that was navigated to.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Whether the navigation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// HTTP status code of the navigation response.
    /// </summary>
    public int HttpStatusCode { get; set; }

    /// <summary>
    /// Page title after navigation completed.
    /// </summary>
    public string? Title { get; set; }
}
