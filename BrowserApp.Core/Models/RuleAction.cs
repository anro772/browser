namespace BrowserApp.Core.Models;

/// <summary>
/// Represents a single action within a rule (block, inject_css, inject_js).
/// </summary>
public class RuleAction
{
    /// <summary>
    /// Type of action: "block", "inject_css", "inject_js".
    /// </summary>
    public string Type { get; set; } = "block";

    /// <summary>
    /// Match conditions for this action.
    /// </summary>
    public RuleMatch Match { get; set; } = new();

    /// <summary>
    /// CSS to inject (for inject_css type).
    /// </summary>
    public string? Css { get; set; }

    /// <summary>
    /// JavaScript to inject (for inject_js type).
    /// </summary>
    public string? Js { get; set; }

    /// <summary>
    /// Timing for injection: "dom_ready" or "load".
    /// </summary>
    public string? Timing { get; set; } = "dom_ready";

    /// <summary>
    /// Header modifications to apply (for modify_headers type).
    /// </summary>
    public List<HeaderModification>? Headers { get; set; }

    /// <summary>
    /// Content policy categories to block (for content_policy type).
    /// Example: ["ai_services", "torrents", "adult_content", "gambling"]
    /// </summary>
    public List<string>? Categories { get; set; }
}
