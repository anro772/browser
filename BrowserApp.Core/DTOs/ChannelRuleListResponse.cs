namespace BrowserApp.Core.DTOs;

/// <summary>
/// Response containing a list of channel rules.
/// </summary>
public class ChannelRuleListResponse
{
    public Guid ChannelId { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public List<ChannelRuleResponse> Rules { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}
