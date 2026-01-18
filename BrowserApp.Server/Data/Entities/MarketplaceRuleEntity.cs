namespace BrowserApp.Server.Data.Entities;

/// <summary>
/// Entity for storing marketplace rules in PostgreSQL.
/// Mirrors client RuleEntity structure with marketplace-specific fields.
/// </summary>
public class MarketplaceRuleEntity
{
    /// <summary>
    /// Unique identifier (UUID).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

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
    /// Author user ID (foreign key).
    /// </summary>
    public Guid AuthorId { get; set; }

    /// <summary>
    /// Navigation property to author.
    /// </summary>
    public UserEntity Author { get; set; } = null!;

    /// <summary>
    /// Number of times this rule has been downloaded.
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// When the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the rule was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
