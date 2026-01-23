namespace BrowserApp.Server.Data.Entities;

/// <summary>
/// Entity for storing channels in PostgreSQL.
/// A channel is a collection of rules that users can subscribe to.
/// </summary>
public class ChannelEntity
{
    /// <summary>
    /// Unique identifier (UUID).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable name for the channel.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what rules this channel contains.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Owner user ID (foreign key to Users table).
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Navigation property to owner.
    /// </summary>
    public UserEntity Owner { get; set; } = null!;

    /// <summary>
    /// SHA256 hash of the channel password.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Whether the channel is publicly listed.
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Whether the channel is active (soft delete).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Cached count of members for performance.
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// When the channel was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the channel was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to channel rules.
    /// </summary>
    public ICollection<ChannelRuleEntity> Rules { get; set; } = new List<ChannelRuleEntity>();

    /// <summary>
    /// Navigation property to channel members.
    /// </summary>
    public ICollection<ChannelMemberEntity> Members { get; set; } = new List<ChannelMemberEntity>();
}
