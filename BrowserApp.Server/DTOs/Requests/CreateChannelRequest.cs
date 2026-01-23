using System.ComponentModel.DataAnnotations;

namespace BrowserApp.Server.DTOs.Requests;

/// <summary>
/// Request to create a new channel.
/// </summary>
public class CreateChannelRequest
{
    /// <summary>
    /// Name of the channel.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the channel.
    /// </summary>
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Username of the channel owner.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string OwnerUsername { get; set; } = string.Empty;

    /// <summary>
    /// Password for the channel.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 4)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether the channel is publicly listed.
    /// </summary>
    public bool IsPublic { get; set; } = true;
}
