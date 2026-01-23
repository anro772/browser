using BrowserApp.Core.DTOs;

namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Interface for channel sync service operations.
/// Simplified for MVP - manual sync only (no background timer).
/// </summary>
public interface IChannelSyncService
{
    /// <summary>
    /// Joins a channel and performs initial sync.
    /// </summary>
    Task<bool> JoinChannelAsync(Guid channelId, string channelName, string channelDescription, string username, string password);

    /// <summary>
    /// Leaves a channel and removes local rules.
    /// </summary>
    Task<bool> LeaveChannelAsync(string channelId, string username);

    /// <summary>
    /// Syncs rules for a specific channel.
    /// </summary>
    Task<bool> SyncChannelRulesAsync(string channelId, string username);

    /// <summary>
    /// Syncs all joined channels.
    /// </summary>
    Task SyncAllChannelsAsync(string username);

    /// <summary>
    /// Gets all active channel memberships.
    /// </summary>
    Task<IEnumerable<ChannelMembershipDto>> GetJoinedChannelsAsync();

    /// <summary>
    /// Checks if the server is available.
    /// </summary>
    Task<bool> IsServerAvailableAsync();

    /// <summary>
    /// Saves a local membership record (used when owner creates channel).
    /// </summary>
    Task SaveLocalMembershipAsync(string channelId, string channelName, string channelDescription, string username);
}
