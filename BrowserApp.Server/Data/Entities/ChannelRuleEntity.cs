namespace BrowserApp.Server.Data.Entities;

/// <summary>
/// Entity for storing rules within a channel.
/// </summary>
public class ChannelRuleEntity
{
    /// <summary>
    /// Unique identifier (UUID).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Channel ID (foreign key).
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// Navigation property to channel.
    /// </summary>
    public ChannelEntity Channel { get; set; } = null!;

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
    /// Use "*" for all sites.
    /// </summary>
    public string Site { get; set; } = "*";

    /// <summary>
    /// Priority for rule evaluation (higher = evaluated first).
    /// </summary>
    public int Priority { get; set; } = 10;

    /// <summary>
    /// JSON array of RuleAction objects (stored as JSONB in PostgreSQL).
    /// </summary>
    public string RulesJson { get; set; } = "[]";

    /// <summary>
    /// Whether this rule is enforced (cannot be disabled by users).
    /// </summary>
    public bool IsEnforced { get; set; } = true;

    /// <summary>
    /// When the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the rule was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
