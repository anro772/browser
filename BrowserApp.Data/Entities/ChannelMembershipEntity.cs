namespace BrowserApp.Data.Entities;

/// <summary>
/// Entity for tracking channel memberships locally (SQLite).
/// </summary>
public class ChannelMembershipEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Server channel ID.
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Channel name (cached for offline display).
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// Channel description (cached for offline display).
    /// </summary>
    public string ChannelDescription { get; set; } = string.Empty;

    /// <summary>
    /// Local username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Whether the membership is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the user joined the channel.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When rules were last synced from this channel.
    /// </summary>
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of rules in this channel (cached from last sync).
    /// </summary>
    public int RuleCount { get; set; }
}
