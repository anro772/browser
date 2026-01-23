namespace BrowserApp.Server.DTOs.Responses;

/// <summary>
/// Response containing a list of channel rules.
/// </summary>
public class ChannelRuleListResponse
{
    /// <summary>
    /// Channel ID.
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// Channel name.
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// List of rules in the channel.
    /// </summary>
    public List<ChannelRuleResponse> Rules { get; set; } = new();

    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
