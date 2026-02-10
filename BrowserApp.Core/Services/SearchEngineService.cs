using System.Text.RegularExpressions;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.Core.Services;

/// <summary>
/// Service for detecting URLs vs search queries and building navigation URLs.
/// Supports multiple search engines and custom search URLs.
/// </summary>
public partial class SearchEngineService : ISearchEngineService
{
    private static readonly Dictionary<string, (string SearchUrl, string HomeUrl)> EngineMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Google"] = ("https://www.google.com/search?q=", "https://www.google.com"),
        ["Bing"] = ("https://www.bing.com/search?q=", "https://www.bing.com"),
        ["DuckDuckGo"] = ("https://duckduckgo.com/?q=", "https://duckduckgo.com"),
        ["Brave"] = ("https://search.brave.com/search?q=", "https://search.brave.com"),
        ["Ecosia"] = ("https://www.ecosia.org/search?q=", "https://www.ecosia.org"),
    };

    private string _currentEngine = "Google";
    private string _searchUrl = "https://www.google.com/search?q=";
    private string _homeUrl = "https://www.google.com";
    private string? _customUrlTemplate;

    // Regex for IP address detection
    [GeneratedRegex(@"^(\d{1,3}\.){3}\d{1,3}(:\d+)?(/.*)?$")]
    private static partial Regex IpAddressRegex();

    // Regex for localhost detection
    [GeneratedRegex(@"^localhost(:\d+)?(/.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex LocalhostRegex();

    /// <inheritdoc/>
    public IReadOnlyList<string> AvailableEngines { get; } = new List<string>(EngineMap.Keys).Concat(new[] { "Custom" }).ToList().AsReadOnly();

    /// <inheritdoc/>
    public string CurrentEngine => _currentEngine;

    /// <inheritdoc/>
    public void SetSearchEngine(string engineName)
    {
        if (engineName == "Custom" && _customUrlTemplate != null)
        {
            _currentEngine = "Custom";
            return;
        }

        if (EngineMap.TryGetValue(engineName, out var engine))
        {
            _currentEngine = engineName;
            _searchUrl = engine.SearchUrl;
            _homeUrl = engine.HomeUrl;
            _customUrlTemplate = null;
        }
    }

    /// <inheritdoc/>
    public void SetCustomSearchEngine(string urlTemplate)
    {
        if (!string.IsNullOrWhiteSpace(urlTemplate))
        {
            _customUrlTemplate = urlTemplate;
            _currentEngine = "Custom";
            _searchUrl = urlTemplate.Replace("{query}", "");
            _homeUrl = "about:blank";
        }
    }

    /// <inheritdoc/>
    public string GetHomePageUrl() => _homeUrl;

    /// <inheritdoc/>
    public string GetNavigationUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return _homeUrl;
        }

        input = input.Trim();

        // Already has protocol
        if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        // Check for localhost - use HTTP
        if (LocalhostRegex().IsMatch(input))
        {
            return "http://" + input;
        }

        // Check for IP address - use HTTP
        if (IpAddressRegex().IsMatch(input))
        {
            return "http://" + input;
        }

        // Check if it looks like a URL (has dot and no spaces)
        if (IsValidUrl(input))
        {
            return "https://" + input;
        }

        // Otherwise, it's a search query
        return BuildSearchUrl(input);
    }

    /// <inheritdoc/>
    public bool IsValidUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim();

        // Already has protocol - it's a URL
        if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Localhost is a URL
        if (LocalhostRegex().IsMatch(input))
        {
            return true;
        }

        // IP address is a URL
        if (IpAddressRegex().IsMatch(input))
        {
            return true;
        }

        // Contains spaces = search query
        if (input.Contains(' '))
        {
            return false;
        }

        // Contains a dot (domain-like) and no spaces
        if (input.Contains('.'))
        {
            // Try to validate as URI
            return Uri.TryCreate("https://" + input, UriKind.Absolute, out _);
        }

        return false;
    }

    private string BuildSearchUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);

        if (_customUrlTemplate != null)
        {
            return _customUrlTemplate.Replace("{query}", encodedQuery);
        }

        return _searchUrl + encodedQuery;
    }
}
