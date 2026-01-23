namespace BrowserApp.Server.Data.Entities;

/// <summary>
/// Entity for tracking channel memberships.
/// </summary>
public class ChannelMemberEntity
{
    /// <summary>
    /// Unique identifier (UUID).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Channel ID (foreign key).
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// Navigation property to channel.
    /// </summary>
    public ChannelEntity Channel { get; set; } = null!;

    /// <summary>
    /// User ID (foreign key).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to user.
    /// </summary>
    public UserEntity User { get; set; } = null!;

    /// <summary>
    /// When the user joined the channel.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user last synced rules from this channel.
    /// </summary>
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
}
