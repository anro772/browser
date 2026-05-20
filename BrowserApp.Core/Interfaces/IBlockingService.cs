using BrowserApp.Core.Models;

namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Service for coordinating request blocking decisions.
/// </summary>
public interface IBlockingService
{
    /// <summary>
    /// Raised whenever a request is blocked. Used by the privacy dashboard to refresh stats.
    /// </summary>
    event EventHandler<NetworkRequest>? RequestBlocked;

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
    /// Gets the total number of requests evaluated this session (blocked + allowed).
    /// Useful for the dashboard to display the blocked-vs-detected ratio.
    /// </summary>
    int GetDetectedCount();

    /// <summary>
    /// Gets the total bytes saved by blocking requests.
    /// </summary>
    long GetBytesSaved();

    /// <summary>
    /// Records a block that was performed outside <see cref="ShouldBlockRequest"/> — typically
    /// by FilterListService (EasyList/EasyPrivacy) inside the request interceptor. Increments
    /// session counters and raises <see cref="RequestBlocked"/> so the privacy dashboard sees
    /// the block. Without this hook FilterListService blocks only show up in the network monitor.
    /// </summary>
    void RecordExternalBlock(NetworkRequest request);

    /// <summary>
    /// Resets the session statistics.
    /// </summary>
    void ResetStats();
}
