using BrowserApp.Server.DTOs.Requests;
using BrowserApp.Server.DTOs.Responses;

namespace BrowserApp.Server.Interfaces;

/// <summary>
/// Service interface for marketplace operations.
/// </summary>
public interface IMarketplaceService
{
    /// <summary>
    /// Gets a paginated list of all rules.
    /// </summary>
    Task<RuleListResponse> GetRulesAsync(int page, int pageSize);

    /// <summary>
    /// Gets a rule by its ID.
    /// </summary>
    Task<RuleResponse?> GetRuleByIdAsync(Guid id);

    /// <summary>
    /// Uploads a new rule to the marketplace.
    /// </summary>
    Task<RuleResponse> UploadRuleAsync(RuleUploadRequest request);

    /// <summary>
    /// Searches rules by query and tags.
    /// </summary>
    Task<RuleListResponse> SearchRulesAsync(string? query, string[]? tags, int page, int pageSize);

    /// <summary>
    /// Increments the download count for a rule and returns the updated rule.
    /// </summary>
    Task<RuleResponse?> IncrementDownloadAsync(Guid id);

    /// <summary>
    /// Deletes a rule by its ID.
    /// </summary>
    Task<bool> DeleteRuleAsync(Guid id);
}
