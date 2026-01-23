namespace BrowserApp.Core.DTOs;

/// <summary>
/// Response containing channel rule details.
/// </summary>
public class ChannelRuleResponse
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Site { get; set; } = "*";
    public int Priority { get; set; }
    public string RulesJson { get; set; } = "[]";
    public bool IsEnforced { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
