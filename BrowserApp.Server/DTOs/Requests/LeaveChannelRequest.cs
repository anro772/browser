using System.ComponentModel.DataAnnotations;

namespace BrowserApp.Server.DTOs.Requests;

/// <summary>
/// Request to leave a channel.
/// </summary>
public class LeaveChannelRequest
{
    /// <summary>
    /// Username of the user leaving.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;
}
