using BrowserApp.Server.Data.Entities;

namespace BrowserApp.Server.Interfaces;

/// <summary>
/// Repository interface for marketplace rule operations.
/// </summary>
public interface IMarketplaceRuleRepository
{
    /// <summary>
    /// Gets all rules with pagination.
    /// </summary>
    Task<IEnumerable<MarketplaceRuleEntity>> GetPagedAsync(int page, int pageSize);

    /// <summary>
    /// Gets a rule by its ID.
    /// </summary>
    Task<MarketplaceRuleEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new rule.
    /// </summary>
    Task<MarketplaceRuleEntity> AddAsync(MarketplaceRuleEntity rule);

    /// <summary>
    /// Updates an existing rule.
    /// </summary>
    Task<MarketplaceRuleEntity> UpdateAsync(MarketplaceRuleEntity rule);

    /// <summary>
    /// Deletes a rule by its ID.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Searches rules by query and tags.
    /// </summary>
    Task<IEnumerable<MarketplaceRuleEntity>> SearchAsync(string? query, string[]? tags, int page, int pageSize);

    /// <summary>
    /// Gets the total count of rules.
    /// </summary>
    Task<int> GetTotalCountAsync();

    /// <summary>
    /// Gets the count of rules matching the search.
    /// </summary>
    Task<int> GetSearchCountAsync(string? query, string[]? tags);

    /// <summary>
    /// Increments the download count for a rule.
    /// </summary>
    Task<MarketplaceRuleEntity?> IncrementDownloadCountAsync(Guid id);
}
