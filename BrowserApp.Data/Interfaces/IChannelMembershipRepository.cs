using BrowserApp.Data.Entities;

namespace BrowserApp.Data.Interfaces;

/// <summary>
/// Repository interface for channel membership operations.
/// </summary>
public interface IChannelMembershipRepository
{
    /// <summary>
    /// Gets all active memberships.
    /// </summary>
    Task<IEnumerable<ChannelMembershipEntity>> GetActiveAsync();

    /// <summary>
    /// Gets a membership by ID.
    /// </summary>
    Task<ChannelMembershipEntity?> GetByIdAsync(string id);

    /// <summary>
    /// Gets a membership by channel ID.
    /// </summary>
    Task<ChannelMembershipEntity?> GetByChannelIdAsync(string channelId);

    /// <summary>
    /// Adds a new membership.
    /// </summary>
    Task AddAsync(ChannelMembershipEntity entity);

    /// <summary>
    /// Updates a membership.
    /// </summary>
    Task UpdateAsync(ChannelMembershipEntity entity);

    /// <summary>
    /// Deletes a membership.
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Deletes a membership by channel ID.
    /// </summary>
    Task DeleteByChannelIdAsync(string channelId);

    /// <summary>
    /// Updates the last synced time for a membership.
    /// </summary>
    Task UpdateLastSyncedAsync(string channelId, int ruleCount);
}
