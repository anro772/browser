using BrowserApp.Core.Utilities;

namespace BrowserApp.Core.Models;

/// <summary>
/// Represents a complete rule set with multiple actions.
/// </summary>
public class Rule
{
    /// <summary>
    /// Unique identifier (GUID as string).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable name for the rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the rule does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// URL pattern for sites this rule applies to.
    /// Use "*" for all sites, "*.example.com" for specific domains.
    /// </summary>
    public string Site { get; set; } = "*";

    /// <summary>
    /// Whether the rule is currently active.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Priority for rule evaluation (higher = evaluated first).
    /// </summary>
    public int Priority { get; set; } = 10;

    /// <summary>
    /// Collection of rule actions (block, inject_css, inject_js).
    /// </summary>
    public List<RuleAction> Rules { get; set; } = new();

    /// <summary>
    /// Source of the rule: "local", "marketplace", "channel", "template".
    /// </summary>
    public string Source { get; set; } = "local";

    /// <summary>
    /// Channel ID if this rule came from a business channel.
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    /// Whether this rule is enforced by a channel (cannot be disabled).
    /// </summary>
    public bool IsEnforced { get; set; }

    /// <summary>
    /// When the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the rule was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Checks if this rule applies to the given page URL.
    /// The Site field is a domain pattern (e.g., "*.1337x.to" or "1337x.to").
    /// It is matched against the hostname extracted from the page URL.
    /// </summary>
    public bool AppliesTo(string pageUrl)
    {
        if (string.IsNullOrEmpty(Site) || Site == "*")
            return true;

        // First try matching against the full URL (backwards-compatible)
        if (UrlMatcher.Matches(pageUrl, Site))
            return true;

        // Extract hostname from URL and match the site pattern against it
        try
        {
            if (Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri))
            {
                var host = uri.Host; // e.g., "1337x.to" or "www.1337x.to"

                // Direct match of site pattern against hostname
                if (UrlMatcher.Matches(host, Site))
                    return true;

                // Handle "*.domain.tld" patterns matching bare "domain.tld" (no subdomain)
                // e.g., "*.1337x.to" should also match "1337x.to" itself
                if (Site.StartsWith("*."))
                {
                    var domainPart = Site.Substring(2); // "1337x.to"
                    if (host.Equals(domainPart, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (host.EndsWith("." + domainPart, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }
        catch
        {
            // If URL parsing fails, fall through to false
        }

        return false;
    }

    /// <summary>
    /// Gets all block actions from this rule.
    /// </summary>
    public IEnumerable<RuleAction> GetBlockActions() =>
        Rules.Where(r => r.Type == "block");

    /// <summary>
    /// Gets all CSS injection actions from this rule.
    /// </summary>
    public IEnumerable<RuleAction> GetCssInjections() =>
        Rules.Where(r => r.Type == "inject_css");

    /// <summary>
    /// Gets all JavaScript injection actions from this rule.
    /// </summary>
    public IEnumerable<RuleAction> GetJsInjections() =>
        Rules.Where(r => r.Type == "inject_js");
}
