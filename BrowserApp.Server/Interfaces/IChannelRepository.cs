using BrowserApp.Server.Data.Entities;

namespace BrowserApp.Server.Interfaces;

/// <summary>
/// Repository interface for channel operations.
/// </summary>
public interface IChannelRepository
{
    // Channel CRUD
    Task<ChannelEntity?> GetByIdAsync(Guid id);
    Task<ChannelEntity?> GetByIdWithRulesAsync(Guid id);
    Task<IEnumerable<ChannelEntity>> GetPagedAsync(int page, int pageSize);
    Task<int> GetCountAsync();
    Task<IEnumerable<ChannelEntity>> SearchAsync(string? query, int page, int pageSize);
    Task<int> GetSearchCountAsync(string? query);
    Task<ChannelEntity> AddAsync(ChannelEntity entity);
    Task UpdateAsync(ChannelEntity entity);
    Task DeleteAsync(Guid id);

    // Channel Rules
    Task<IEnumerable<ChannelRuleEntity>> GetChannelRulesAsync(Guid channelId);
    Task<ChannelRuleEntity?> GetChannelRuleByIdAsync(Guid ruleId);
    Task<ChannelRuleEntity> AddChannelRuleAsync(ChannelRuleEntity rule);
    Task UpdateChannelRuleAsync(ChannelRuleEntity rule);
    Task DeleteChannelRuleAsync(Guid ruleId);

    // Membership
    Task<ChannelMemberEntity?> GetMemberAsync(Guid channelId, Guid userId);
    Task<IEnumerable<ChannelMemberEntity>> GetMembersAsync(Guid channelId);
    Task<IEnumerable<ChannelEntity>> GetUserChannelsAsync(Guid userId);
    Task<ChannelMemberEntity> AddMemberAsync(ChannelMemberEntity member);
    Task RemoveMemberAsync(Guid channelId, Guid userId);
    Task UpdateMemberSyncTimeAsync(Guid channelId, Guid userId);

    // Counters
    Task IncrementMemberCountAsync(Guid channelId);
    Task DecrementMemberCountAsync(Guid channelId);
}
