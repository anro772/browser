using BrowserApp.Core.DTOs;

namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Interface for marketplace API client operations.
/// </summary>
public interface IMarketplaceApiClient
{
    /// <summary>
    /// Gets a paginated list of all marketplace rules.
    /// </summary>
    Task<RuleListResponse?> GetRulesAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets a rule by its ID.
    /// </summary>
    Task<RuleResponse?> GetRuleByIdAsync(Guid id);

    /// <summary>
    /// Uploads a rule to the marketplace.
    /// </summary>
    Task<RuleResponse?> UploadRuleAsync(RuleUploadRequest request);

    /// <summary>
    /// Searches marketplace rules.
    /// </summary>
    Task<RuleListResponse?> SearchRulesAsync(string? query, string[]? tags, int page = 1, int pageSize = 20);

    /// <summary>
    /// Increments the download count for a rule.
    /// </summary>
    Task<RuleResponse?> IncrementDownloadAsync(Guid id);

    /// <summary>
    /// Checks if the marketplace server is available.
    /// </summary>
    Task<bool> CheckConnectionAsync();

    /// <summary>
    /// Gets the configured server URL.
    /// </summary>
    string? ServerUrl { get; }
}
