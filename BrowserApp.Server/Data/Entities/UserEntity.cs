namespace BrowserApp.Server.Data.Entities;

/// <summary>
/// User entity for marketplace attribution.
/// Simple username-only (no authentication in Phase 4).
/// </summary>
public class UserEntity
{
    /// <summary>
    /// Unique identifier (UUID).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Username for display and attribution.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// When the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Rules uploaded by this user.
    /// </summary>
    public List<MarketplaceRuleEntity> Rules { get; set; } = new();
}
