namespace BrowserApp.UI.DTOs;

/// <summary>
/// Response DTO for a single marketplace rule.
/// </summary>
public class RuleResponse
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the rule does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// URL pattern for sites this rule applies to.
    /// </summary>
    public string Site { get; set; } = "*";

    /// <summary>
    /// Priority for rule evaluation.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// JSON array of RuleAction objects.
    /// </summary>
    public string RulesJson { get; set; } = "[]";

    /// <summary>
    /// Username of the author.
    /// </summary>
    public string AuthorUsername { get; set; } = string.Empty;

    /// <summary>
    /// Number of downloads.
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// When the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the rule was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
