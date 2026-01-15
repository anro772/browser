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
    /// <param name="input">User input from address bar.</param>
    /// <returns>A fully qualified URL for navigation.</returns>
    string GetNavigationUrl(string input);

    /// <summary>
    /// Checks if the input appears to be a valid URL.
    /// </summary>
    /// <param name="input">User input to check.</param>
    /// <returns>True if input looks like a URL, false if it's a search query.</returns>
    bool IsValidUrl(string input);
}
