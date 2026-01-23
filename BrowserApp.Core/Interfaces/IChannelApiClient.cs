namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Interface for channel API client operations.
/// </summary>
public interface IChannelApiClient
{
    /// <summary>
    /// Gets a paginated list of public channels.
    /// </summary>
    Task<object?> GetChannelsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets a specific channel by ID.
    /// </summary>
    Task<object?> GetChannelByIdAsync(Guid channelId);

    /// <summary>
    /// Creates a new channel.
    /// </summary>
    Task<object?> CreateChannelAsync(string name, string description, string ownerUsername, string password, bool isPublic = true);

    /// <summary>
    /// Joins a channel with password.
    /// </summary>
    Task<bool> JoinChannelAsync(Guid channelId, string username, string password);

    /// <summary>
    /// Leaves a channel.
    /// </summary>
    Task<bool> LeaveChannelAsync(Guid channelId, string username);

    /// <summary>
    /// Gets channels the user has joined.
    /// </summary>
    Task<object?> GetUserChannelsAsync(string username);

    /// <summary>
    /// Gets rules for a channel (requires membership).
    /// </summary>
    Task<object?> GetChannelRulesAsync(Guid channelId, string username);

    /// <summary>
    /// Checks if the server is available.
    /// </summary>
    Task<bool> CheckConnectionAsync();
}
