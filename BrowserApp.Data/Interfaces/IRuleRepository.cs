using BrowserApp.Data.Entities;

namespace BrowserApp.Data.Interfaces;

/// <summary>
/// Repository interface for rule CRUD operations.
/// </summary>
public interface IRuleRepository
{
    /// <summary>
    /// Gets all rules.
    /// </summary>
    Task<IEnumerable<RuleEntity>> GetAllAsync();

    /// <summary>
    /// Gets all enabled rules ordered by priority descending.
    /// </summary>
    Task<IEnumerable<RuleEntity>> GetEnabledAsync();

    /// <summary>
    /// Gets a rule by its ID.
    /// </summary>
    Task<RuleEntity?> GetByIdAsync(string id);

    /// <summary>
    /// Adds a new rule.
    /// </summary>
    Task AddAsync(RuleEntity rule);

    /// <summary>
    /// Updates an existing rule.
    /// </summary>
    Task UpdateAsync(RuleEntity rule);

    /// <summary>
    /// Deletes a rule by ID.
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Gets the total count of rules.
    /// </summary>
    Task<int> GetCountAsync();

    /// <summary>
    /// Gets rules by source type.
    /// </summary>
    Task<IEnumerable<RuleEntity>> GetBySourceAsync(string source);

    /// <summary>
    /// Checks if a rule with the given ID exists.
    /// </summary>
    Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Deletes all rules from a specific channel.
    /// </summary>
    Task DeleteByChannelIdAsync(string channelId);
}
