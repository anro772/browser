namespace BrowserApp.Core.DTOs;

/// <summary>
/// DTO for channel membership information.
/// </summary>
public class ChannelMembershipDto
{
    public string Id { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelDescription { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public int RuleCount { get; set; }
}
