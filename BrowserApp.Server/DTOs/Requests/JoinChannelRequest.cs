using System.ComponentModel.DataAnnotations;

namespace BrowserApp.Server.DTOs.Requests;

/// <summary>
/// Request to join a channel.
/// </summary>
public class JoinChannelRequest
{
    /// <summary>
    /// Username of the user joining.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for the channel.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
