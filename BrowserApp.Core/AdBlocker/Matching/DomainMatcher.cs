using System.Diagnostics;

namespace BrowserApp.Core.AdBlocker.Matching;

/// <summary>
/// Fast O(1) exact domain matching using HashSet.
/// Handles the majority (80%+) of ad blocking with minimal overhead.
/// </summary>
public class DomainMatcher
{
    private readonly HashSet<string> _exactDomains = new();
    private readonly HashSet<string> _exactUrls = new();

    /// <summary>
    /// Adds an exact domain to the matcher.
    /// </summary>
    public void AddDomain(string domain)
    {
        _exactDomains.Add(domain.ToLowerInvariant());
    }

    /// <summary>
    /// Adds an exact URL to the matcher.
    /// </summary>
    public void AddUrl(string url)
    {
        _exactUrls.Add(url.ToLowerInvariant());
    }

    /// <summary>
    /// Checks if a URL matches any exact domain or URL.
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <returns>True if blocked, false otherwise</returns>
    public bool IsMatch(string url)
    {
        var lowerUrl = url.ToLowerInvariant();

        // Check exact URL match first (fastest)
        if (_exactUrls.Contains(lowerUrl))
        {
            return true;
        }

        // Extract domain from URL
        var domain = ExtractDomain(lowerUrl);
        if (domain == null)
        {
            return false;
        }

        // Check exact domain match
        if (_exactDomains.Contains(domain))
        {
            return true;
        }

        // Check parent domains (subdomain matching)
        // e.g., "ads.example.com" should match "example.com"
        var parts = domain.Split('.');
        for (int i = 1; i < parts.Length; i++)
        {
            var parentDomain = string.Join(".", parts.Skip(i));
            if (_exactDomains.Contains(parentDomain))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts the domain from a URL.
    /// </summary>
    private string? ExtractDomain(string url)
    {
        try
        {
            // Handle protocol-relative URLs
            if (url.StartsWith("//"))
            {
                url = "https:" + url;
            }

            // Try to parse as URI
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Host.ToLowerInvariant();
            }

            // If not a valid URI, try to extract domain manually
            // Remove protocol
            var withoutProtocol = url;
            if (url.Contains("://"))
            {
                withoutProtocol = url.Substring(url.IndexOf("://") + 3);
            }

            // Extract until first / or ?
            var firstSlash = withoutProtocol.IndexOfAny(new[] { '/', '?' });
            if (firstSlash > 0)
            {
                withoutProtocol = withoutProtocol.Substring(0, firstSlash);
            }

            // Remove port if present
            var colonIndex = withoutProtocol.IndexOf(':');
            if (colonIndex > 0)
            {
                withoutProtocol = withoutProtocol.Substring(0, colonIndex);
            }

            return string.IsNullOrWhiteSpace(withoutProtocol) ? null : withoutProtocol.ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }

    public int Count => _exactDomains.Count + _exactUrls.Count;
}
