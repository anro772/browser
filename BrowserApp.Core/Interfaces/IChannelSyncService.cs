namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Interface for channel sync service operations.
/// </summary>
public interface IChannelSyncService
{
    /// <summary>
    /// Starts the background sync service.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stops the background sync service.
    /// </summary>
    Task StopAsync();

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
    Task<IEnumerable<object>> GetJoinedChannelsAsync();

    /// <summary>
    /// Checks if the server is available.
    /// </summary>
    Task<bool> IsServerAvailableAsync();
}
