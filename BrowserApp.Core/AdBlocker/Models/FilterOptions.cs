namespace BrowserApp.Core.AdBlocker.Models;

/// <summary>
/// Options for filter rules (resource types, third-party, domain restrictions).
/// </summary>
public class FilterOptions
{
    /// <summary>
    /// Allowed resource types. If empty, all types are allowed.
    /// Examples: script, image, stylesheet, xmlhttprequest, subdocument
    /// </summary>
    public HashSet<string> AllowedResourceTypes { get; set; } = new();

    /// <summary>
    /// Blocked resource types. Takes precedence over AllowedResourceTypes.
    /// </summary>
    public HashSet<string> BlockedResourceTypes { get; set; } = new();

    /// <summary>
    /// Only match if request is third-party (from different domain than page).
    /// </summary>
    public bool? ThirdPartyOnly { get; set; }

    /// <summary>
    /// Domains where this rule applies. If empty, applies to all domains.
    /// Example: ["example.com", "test.com"]
    /// </summary>
    public HashSet<string> ApplicableDomains { get; set; } = new();

    /// <summary>
    /// Domains where this rule does NOT apply.
    /// Example: ["news.example.com"]
    /// </summary>
    public HashSet<string> ExcludedDomains { get; set; } = new();

    /// <summary>
    /// Whether this filter has any options specified.
    /// </summary>
    public bool HasOptions =>
        AllowedResourceTypes.Count > 0 ||
        BlockedResourceTypes.Count > 0 ||
        ThirdPartyOnly.HasValue ||
        ApplicableDomains.Count > 0 ||
        ExcludedDomains.Count > 0;
}
