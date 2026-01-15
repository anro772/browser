namespace BrowserApp.Data.Entities;

/// <summary>
/// Entity for storing rules in SQLite database.
/// </summary>
public class RuleEntity
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
    /// JSON array of RuleAction objects.
    /// </summary>
    public string RulesJson { get; set; } = "[]";

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
}
