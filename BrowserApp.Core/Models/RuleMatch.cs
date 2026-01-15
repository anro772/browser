namespace BrowserApp.Core.Models;

/// <summary>
/// Defines match conditions for a rule action.
/// </summary>
public class RuleMatch
{
    /// <summary>
    /// URL pattern with wildcards (* matches anything).
    /// Examples: "*tracker.com/*", "*.google-analytics.com/*"
    /// </summary>
    public string? UrlPattern { get; set; }

    /// <summary>
    /// Resource type to match (script, stylesheet, image, xhr, fetch, document, font, media, websocket).
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// HTTP method to match (GET, POST, etc.).
    /// </summary>
    public string? Method { get; set; }
}
