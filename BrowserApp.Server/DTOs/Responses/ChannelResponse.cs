namespace BrowserApp.Server.DTOs.Responses;

/// <summary>
/// Response containing channel details.
/// </summary>
public class ChannelResponse
{
    /// <summary>
    /// Channel ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the channel.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the channel.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Username of the channel owner.
    /// </summary>
    public string OwnerUsername { get; set; } = string.Empty;

    /// <summary>
    /// Whether the channel is publicly listed.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Number of members in the channel.
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Number of rules in the channel.
    /// </summary>
    public int RuleCount { get; set; }

    /// <summary>
    /// When the channel was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the channel was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
