using System.Text.RegularExpressions;

namespace BrowserApp.Core.Utilities;

/// <summary>
/// Utility for matching URLs against wildcard patterns.
/// </summary>
public static class UrlMatcher
{
    private static readonly Dictionary<string, Regex> _regexCache = new();

    /// <summary>
    /// Checks if a URL matches the given wildcard pattern.
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <param name="pattern">Wildcard pattern (* matches anything).</param>
    /// <returns>True if the URL matches the pattern.</returns>
    /// <remarks>
    /// Pattern examples:
    /// - "*" matches all URLs
    /// - "*.example.com/*" matches any subdomain of example.com
    /// - "*tracker.com/*" matches any URL containing tracker.com
    /// - "https://example.com/page*" matches URLs starting with that prefix
    /// </remarks>
    public static bool Matches(string url, string pattern)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(pattern))
            return false;

        // Universal wildcard matches everything
        if (pattern == "*")
            return true;

        try
        {
            var regex = GetOrCreateRegex(pattern);
            return regex.IsMatch(url);
        }
        catch
        {
            // Invalid pattern - fail safe (no match)
            return false;
        }
    }

    /// <summary>
    /// Checks if a resource type matches the filter.
    /// </summary>
    public static bool MatchesResourceType(string requestType, string? filterType)
    {
        if (string.IsNullOrEmpty(filterType))
            return true; // No filter means match all

        return requestType.Equals(filterType, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if an HTTP method matches the filter.
    /// </summary>
    public static bool MatchesMethod(string requestMethod, string? filterMethod)
    {
        if (string.IsNullOrEmpty(filterMethod))
            return true; // No filter means match all

        return requestMethod.Equals(filterMethod, StringComparison.OrdinalIgnoreCase);
    }

    private static Regex GetOrCreateRegex(string pattern)
    {
        if (_regexCache.TryGetValue(pattern, out var cached))
            return cached;

        var regexPattern = ConvertWildcardToRegex(pattern);
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Cache with size limit
        if (_regexCache.Count < 1000)
        {
            _regexCache[pattern] = regex;
        }

        return regex;
    }

    private static string ConvertWildcardToRegex(string pattern)
    {
        // Escape regex special characters except *
        var escaped = Regex.Escape(pattern);

        // Convert escaped \* back to .* (wildcard)
        escaped = escaped.Replace("\\*", ".*");

        // Make it match the full string
        return $"^{escaped}$";
    }

    /// <summary>
    /// Clears the regex cache (useful for testing).
    /// </summary>
    public static void ClearCache()
    {
        _regexCache.Clear();
    }
}
