namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Service for detecting URLs vs search queries and building navigation URLs.
/// </summary>
public interface ISearchEngineService
{
    /// <summary>
    /// Converts user input into a navigable URL.
    /// If input is a URL, formats it properly.
    /// If input is a search query, returns a search engine URL.
    /// </summary>
    string GetNavigationUrl(string input);

    /// <summary>
    /// Checks if the input appears to be a valid URL.
    /// </summary>
    bool IsValidUrl(string input);

    /// <summary>
    /// Sets the active search engine by name.
    /// </summary>
    void SetSearchEngine(string engineName);

    /// <summary>
    /// Sets a custom search engine URL with {query} placeholder.
    /// </summary>
    void SetCustomSearchEngine(string urlTemplate);

    /// <summary>
    /// Gets the home page URL for the current search engine.
    /// </summary>
    string GetHomePageUrl();

    /// <summary>
    /// Gets the list of available built-in search engine names.
    /// </summary>
    IReadOnlyList<string> AvailableEngines { get; }

    /// <summary>
    /// Gets the current search engine name.
    /// </summary>
    string CurrentEngine { get; }
}
