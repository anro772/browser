using BrowserApp.Server.Data.Entities;
using BrowserApp.Server.DTOs.Requests;
using BrowserApp.Server.DTOs.Responses;

namespace BrowserApp.Server.Mappers;

/// <summary>
/// Static mapper for converting between entities and DTOs.
/// </summary>
public static class MarketplaceRuleMapper
{
    /// <summary>
    /// Converts an entity to a response DTO.
    /// </summary>
    public static RuleResponse ToDto(MarketplaceRuleEntity entity)
    {
        return new RuleResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Site = entity.Site,
            Priority = entity.Priority,
            RulesJson = entity.RulesJson,
            AuthorUsername = entity.Author?.Username ?? "Unknown",
            DownloadCount = entity.DownloadCount,
            Tags = entity.Tags ?? Array.Empty<string>(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// Converts a request DTO to an entity.
    /// </summary>
    public static MarketplaceRuleEntity ToEntity(RuleUploadRequest request, Guid authorId)
    {
        return new MarketplaceRuleEntity
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Site = request.Site,
            Priority = request.Priority,
            RulesJson = request.RulesJson,
            AuthorId = authorId,
            Tags = request.Tags ?? Array.Empty<string>(),
            DownloadCount = 0
        };
    }

    /// <summary>
    /// Converts a list of entities to a paginated response.
    /// </summary>
    public static RuleListResponse ToListDto(
        IEnumerable<MarketplaceRuleEntity> entities,
        int totalCount,
        int page,
        int pageSize)
    {
        return new RuleListResponse
        {
            Rules = entities.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
