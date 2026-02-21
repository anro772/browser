using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace BrowserApp.UI.Services;

/// <summary>
/// Content policy engine that blocks websites by category.
/// Loads category definitions from embedded categories.json resource.
/// </summary>
public class ContentPolicyService
{
    private Dictionary<string, CategoryDefinition> _categories = new();
    private bool _isInitialized;

    /// <summary>
    /// Loads category definitions from the embedded resource.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "BrowserApp.UI.Resources.ContentPolicies.categories.json";

            string? json = null;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                json = await reader.ReadToEndAsync();
            }
            else
            {
                // Fall back to file system
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var filePath = Path.Combine(basePath, "Resources", "ContentPolicies", "categories.json");
                if (File.Exists(filePath))
                {
                    json = await File.ReadAllTextAsync(filePath);
                }
            }

            if (!string.IsNullOrEmpty(json))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var root = JsonSerializer.Deserialize<CategoriesRoot>(json, options);
                if (root?.Categories != null)
                {
                    _categories = root.Categories;
                }
            }

            _isInitialized = true;
            ErrorLogger.LogInfo($"[ContentPolicyService] Loaded {_categories.Count} categories with {_categories.Values.Sum(c => c.Domains?.Count ?? 0)} total domains");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("[ContentPolicyService] Failed to load categories", ex);
            _isInitialized = true; // Mark as initialized even on error to avoid retry loops
        }
    }

    /// <summary>
    /// Checks if a URL is blocked by any of the specified categories.
    /// </summary>
    public bool IsBlocked(string url, List<string> blockedCategories)
    {
        return GetBlockedCategory(url, blockedCategories) != null;
    }

    /// <summary>
    /// Returns the category key that blocks the given URL, or null if not blocked.
    /// </summary>
    public string? GetBlockedCategory(string url, List<string> blockedCategories)
    {
        if (blockedCategories == null || blockedCategories.Count == 0)
            return null;

        string? hostname = ExtractHostname(url);
        if (string.IsNullOrEmpty(hostname))
            return null;

        foreach (var categoryKey in blockedCategories)
        {
            if (!_categories.TryGetValue(categoryKey, out var category))
                continue;

            if (category.Domains == null)
                continue;

            foreach (var domain in category.Domains)
            {
                if (MatchesDomain(hostname, domain))
                {
                    Debug.WriteLine($"[ContentPolicyService] Blocked: {url} matched category '{categoryKey}' (domain: {domain})");
                    return categoryKey;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the display name for a category key.
    /// </summary>
    public string GetCategoryDisplayName(string categoryKey)
    {
        if (_categories.TryGetValue(categoryKey, out var category))
            return category.Name ?? categoryKey;
        return categoryKey;
    }

    /// <summary>
    /// Extracts the hostname from a URL string.
    /// </summary>
    private static string? ExtractHostname(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return uri.Host.ToLowerInvariant();
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Checks if a hostname matches a domain pattern.
    /// Supports exact match and subdomain matching.
    /// e.g., "www.facebook.com" matches "facebook.com"
    /// </summary>
    private static bool MatchesDomain(string hostname, string domain)
    {
        var lowerDomain = domain.ToLowerInvariant();

        // Exact match
        if (hostname == lowerDomain)
            return true;

        // Subdomain match: hostname ends with ".domain"
        if (hostname.EndsWith("." + lowerDomain, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private class CategoriesRoot
    {
        public Dictionary<string, CategoryDefinition>? Categories { get; set; }
    }

    private class CategoryDefinition
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<string>? Domains { get; set; }
    }
}
