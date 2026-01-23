namespace BrowserApp.Core.DTOs;

/// <summary>
/// Request to create a new channel.
/// </summary>
public class CreateChannelRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerUsername { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
}
