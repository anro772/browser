using BrowserApp.Core.DTOs;

namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Interface for channel API client operations.
/// </summary>
public interface IChannelApiClient
{
    /// <summary>
    /// Gets a paginated list of public channels.
    /// </summary>
    Task<ChannelListResponse?> GetChannelsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets a specific channel by ID.
    /// </summary>
    Task<ChannelResponse?> GetChannelByIdAsync(Guid channelId);

    /// <summary>
    /// Creates a new channel.
    /// </summary>
    Task<ChannelResponse?> CreateChannelAsync(CreateChannelRequest request);

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
    Task<ChannelListResponse?> GetUserChannelsAsync(string username);

    /// <summary>
    /// Gets rules for a channel (requires membership).
    /// </summary>
    Task<ChannelRuleListResponse?> GetChannelRulesAsync(Guid channelId, string username);

    /// <summary>
    /// Checks if the server is available.
    /// </summary>
    Task<bool> CheckConnectionAsync();

    /// <summary>
    /// Gets the server URL.
    /// </summary>
    string? ServerUrl { get; }
}
