using BrowserApp.Core.Models;

namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Service for coordinating request blocking decisions.
/// </summary>
public interface IBlockingService
{
    /// <summary>
    /// Initializes the blocking service.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Determines if a request should be blocked.
    /// </summary>
    /// <param name="request">The network request to evaluate.</param>
    /// <param name="currentPageUrl">The URL of the current page.</param>
    /// <returns>The evaluation result with blocking decision.</returns>
    RuleEvaluationResult ShouldBlockRequest(NetworkRequest request, string? currentPageUrl);

    /// <summary>
    /// Gets the count of blocked requests in the current session.
    /// </summary>
    int GetBlockedCount();

    /// <summary>
    /// Gets the total bytes saved by blocking requests.
    /// </summary>
    long GetBytesSaved();

    /// <summary>
    /// Resets the session statistics.
    /// </summary>
    void ResetStats();
}
