using BrowserApp.Server.Data.Entities;
using BrowserApp.Server.DTOs.Requests;
using BrowserApp.Server.DTOs.Responses;

namespace BrowserApp.Server.Mappers;

/// <summary>
/// Static mapper for channel entities and DTOs.
/// </summary>
public static class ChannelMapper
{
    /// <summary>
    /// Converts a ChannelEntity to ChannelResponse.
    /// </summary>
    public static ChannelResponse ToDto(ChannelEntity entity)
    {
        return new ChannelResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            OwnerUsername = entity.Owner?.Username ?? "Unknown",
            IsPublic = entity.IsPublic,
            MemberCount = entity.MemberCount,
            RuleCount = entity.Rules?.Count ?? 0,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// Converts a list of ChannelEntity to ChannelListResponse.
    /// </summary>
    public static ChannelListResponse ToListDto(
        IEnumerable<ChannelEntity> entities,
        int totalCount,
        int page,
        int pageSize)
    {
        return new ChannelListResponse
        {
            Channels = entities.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Converts a CreateChannelRequest to ChannelEntity.
    /// </summary>
    public static ChannelEntity ToEntity(CreateChannelRequest request, Guid ownerId, string passwordHash)
    {
        return new ChannelEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            OwnerId = ownerId,
            PasswordHash = passwordHash,
            IsPublic = request.IsPublic,
            IsActive = true,
            MemberCount = 0, // Will be incremented when owner is added as member
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts a ChannelRuleEntity to ChannelRuleResponse.
    /// </summary>
    public static ChannelRuleResponse ToRuleDto(ChannelRuleEntity entity)
    {
        return new ChannelRuleResponse
        {
            Id = entity.Id,
            ChannelId = entity.ChannelId,
            Name = entity.Name,
            Description = entity.Description,
            Site = entity.Site,
            Priority = entity.Priority,
            RulesJson = entity.RulesJson,
            IsEnforced = entity.IsEnforced,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// Converts a list of ChannelRuleEntity to ChannelRuleListResponse.
    /// </summary>
    public static ChannelRuleListResponse ToRuleListDto(
        ChannelEntity channel,
        IEnumerable<ChannelRuleEntity> rules)
    {
        return new ChannelRuleListResponse
        {
            ChannelId = channel.Id,
            ChannelName = channel.Name,
            Rules = rules.Select(ToRuleDto).ToList(),
            UpdatedAt = channel.UpdatedAt
        };
    }

    /// <summary>
    /// Converts an AddChannelRuleRequest to ChannelRuleEntity.
    /// </summary>
    public static ChannelRuleEntity ToRuleEntity(AddChannelRuleRequest request, Guid channelId)
    {
        return new ChannelRuleEntity
        {
            Id = Guid.NewGuid(),
            ChannelId = channelId,
            Name = request.Name,
            Description = request.Description,
            Site = request.Site,
            Priority = request.Priority,
            RulesJson = request.RulesJson,
            IsEnforced = request.IsEnforced,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
