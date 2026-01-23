namespace BrowserApp.UI.DTOs;

/// <summary>
/// Request to join a channel.
/// </summary>
public class JoinChannelRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
