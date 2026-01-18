namespace BrowserApp.UI.DTOs;

/// <summary>
/// Request DTO for uploading a rule to the marketplace.
/// </summary>
public class RuleUploadRequest
{
    /// <summary>
    /// Human-readable name for the rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the rule does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL pattern for sites this rule applies to.
    /// </summary>
    public string Site { get; set; } = "*";

    /// <summary>
    /// Priority for rule evaluation (higher = evaluated first).
    /// </summary>
    public int Priority { get; set; } = 10;

    /// <summary>
    /// JSON array of RuleAction objects.
    /// </summary>
    public string RulesJson { get; set; } = "[]";

    /// <summary>
    /// Username of the author.
    /// </summary>
    public string AuthorUsername { get; set; } = string.Empty;

    /// <summary>
    /// Optional tags for categorization.
    /// </summary>
    public string[]? Tags { get; set; }
}
