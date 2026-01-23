namespace BrowserApp.Core.DTOs;

/// <summary>
/// Response containing channel details.
/// </summary>
public class ChannelResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerUsername { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int MemberCount { get; set; }
    public int RuleCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
