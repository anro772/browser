using BrowserApp.Server.DTOs.Requests;
using BrowserApp.Server.DTOs.Responses;

namespace BrowserApp.Server.Interfaces;

/// <summary>
/// Service interface for channel business logic.
/// </summary>
public interface IChannelService
{
    // Channel CRUD
    Task<ChannelResponse> CreateChannelAsync(CreateChannelRequest request);
    Task<ChannelResponse?> GetChannelByIdAsync(Guid id);
    Task<ChannelListResponse> GetChannelsAsync(int page, int pageSize);
    Task<ChannelListResponse> SearchChannelsAsync(string? query, int page, int pageSize);
    Task<bool> DeleteChannelAsync(Guid channelId, string username);

    // Membership
    Task<(bool success, string message)> JoinChannelAsync(Guid channelId, JoinChannelRequest request);
    Task<(bool success, string message)> LeaveChannelAsync(Guid channelId, string username);
    Task<ChannelListResponse> GetUserChannelsAsync(string username);

    // Rules
    Task<ChannelRuleListResponse?> GetChannelRulesAsync(Guid channelId, string username);
    Task<ChannelRuleResponse?> AddChannelRuleAsync(Guid channelId, AddChannelRuleRequest request);
    Task<bool> DeleteChannelRuleAsync(Guid channelId, Guid ruleId, string username);

    // Sync
    Task UpdateMemberSyncTimeAsync(Guid channelId, string username);
}
