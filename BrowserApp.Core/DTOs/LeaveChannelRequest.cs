namespace BrowserApp.Core.DTOs;

/// <summary>
/// Request to leave a channel.
/// </summary>
public class LeaveChannelRequest
{
    public string Username { get; set; } = string.Empty;
}
