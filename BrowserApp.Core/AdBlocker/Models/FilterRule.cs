using System.Text.RegularExpressions;

namespace BrowserApp.Core.AdBlocker.Models;

/// <summary>
/// Represents a parsed ad blocking filter rule.
/// </summary>
public class FilterRule
{
    /// <summary>
    /// Type of filter (network or cosmetic).
    /// </summary>
    public FilterType Type { get; set; }

    /// <summary>
    /// Match type for network filters.
    /// </summary>
    public MatchType MatchType { get; set; }

    /// <summary>
    /// The pattern to match against URLs.
    /// For ExactDomain: "doubleclick.net"
    /// For ExactUrl: "https://ads.example.com/banner.js"
    /// For Wildcard: "ad-banner-*.png"
    /// For Regex: compiled Regex object stored separately
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Compiled regex pattern (only for MatchType.Regex).
    /// </summary>
    public Regex? CompiledRegex { get; set; }

    /// <summary>
    /// Whether this is an exception rule (@@).
    /// Exception rules whitelist URLs that would otherwise be blocked.
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// Filter options (resource types, third-party, domain restrictions).
    /// </summary>
    public FilterOptions Options { get; set; } = new();

    /// <summary>
    /// CSS selector for cosmetic filters.
    /// </summary>
    public string? CssSelector { get; set; }

    /// <summary>
    /// Specific domains this rule applies to (for cosmetic filters).
    /// </summary>
    public List<string> ApplicableDomains { get; set; } = new();

    /// <summary>
    /// The raw filter line for debugging.
    /// </summary>
    public string RawFilter { get; set; } = string.Empty;
}

/// <summary>
/// Type of filter rule.
/// </summary>
public enum FilterType
{
    /// <summary>
    /// Network filter that blocks/allows requests.
    /// </summary>
    Network,

    /// <summary>
    /// Cosmetic filter that hides elements via CSS.
    /// </summary>
    Cosmetic
}

/// <summary>
/// How to match the pattern against URLs.
/// </summary>
public enum MatchType
{
    /// <summary>
    /// Exact domain match (||example.com^).
    /// Fastest - O(1) HashSet lookup.
    /// </summary>
    ExactDomain,

    /// <summary>
    /// Exact URL match (|https://example.com/ad.js|).
    /// Fast - O(1) HashSet lookup.
    /// </summary>
    ExactUrl,

    /// <summary>
    /// Wildcard pattern match (ad-banner-*.png).
    /// Fast - O(m) Trie lookup where m = URL length.
    /// </summary>
    Wildcard,

    /// <summary>
    /// Regular expression match (/banner\d+\.jpg/).
    /// Slow - O(n) regex evaluation.
    /// </summary>
    Regex
}
