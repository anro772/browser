namespace BrowserApp.Server.DTOs.Responses;

/// <summary>
/// Response containing channel rule details.
/// </summary>
public class ChannelRuleResponse
{
    /// <summary>
    /// Rule ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Channel ID this rule belongs to.
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// Name of the rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the rule.
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
    /// JSON array of rule actions.
    /// </summary>
    public string RulesJson { get; set; } = "[]";

    /// <summary>
    /// Whether this rule is enforced.
    /// </summary>
    public bool IsEnforced { get; set; }

    /// <summary>
    /// When the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the rule was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
