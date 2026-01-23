namespace BrowserApp.Server.Data.Entities;

/// <summary>
/// Entity for storing channel audit log entries in PostgreSQL.
/// Tracks user actions within channels for analytics and compliance.
/// </summary>
public class ChannelAuditLogEntity
{
    /// <summary>
    /// Unique identifier (UUID).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Channel ID (foreign key to Channels table).
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// Navigation property to channel.
    /// </summary>
    public ChannelEntity Channel { get; set; } = null!;

    /// <summary>
    /// User ID (foreign key to Users table). Nullable for system actions.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Navigation property to user.
    /// </summary>
    public UserEntity? User { get; set; }

    /// <summary>
    /// Action performed (e.g., 'channel_created', 'user_joined', 'user_left', 'rule_added', 'rule_deleted', 'rules_synced').
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// JSON metadata about the action (optional).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
