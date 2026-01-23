using System.ComponentModel.DataAnnotations;

namespace BrowserApp.Server.DTOs.Requests;

/// <summary>
/// Request to add a rule to a channel.
/// </summary>
public class AddChannelRuleRequest
{
    /// <summary>
    /// Username of the user adding the rule (must be owner).
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Name of the rule.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the rule.
    /// </summary>
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// URL pattern for sites this rule applies to.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Site { get; set; } = "*";

    /// <summary>
    /// Priority for rule evaluation.
    /// </summary>
    public int Priority { get; set; } = 10;

    /// <summary>
    /// JSON array of rule actions.
    /// </summary>
    [Required]
    public string RulesJson { get; set; } = "[]";

    /// <summary>
    /// Whether this rule is enforced (cannot be disabled).
    /// </summary>
    public bool IsEnforced { get; set; } = true;
}
