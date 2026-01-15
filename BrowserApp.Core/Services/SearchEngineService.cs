using System.Text.RegularExpressions;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.Core.Services;

/// <summary>
/// Service for detecting URLs vs search queries and building navigation URLs.
/// Supports URL detection, localhost, IP addresses, and search query encoding.
/// </summary>
public partial class SearchEngineService : ISearchEngineService
{
    private const string DefaultSearchEngine = "https://www.google.com/search?q=";

    // Regex for IP address detection
    [GeneratedRegex(@"^(\d{1,3}\.){3}\d{1,3}(:\d+)?(/.*)?$")]
    private static partial Regex IpAddressRegex();

    // Regex for localhost detection
    [GeneratedRegex(@"^localhost(:\d+)?(/.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex LocalhostRegex();

    /// <inheritdoc/>
    public string GetNavigationUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DefaultSearchEngine;
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

    /// <summary>
    /// Builds a search engine URL from a query string.
    /// </summary>
    private static string BuildSearchUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return DefaultSearchEngine + encodedQuery;
    }
}
