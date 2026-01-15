using BrowserApp.Core.Models;

namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Interface for async network request logging.
/// Uses background processing for non-blocking UI.
/// </summary>
public interface INetworkLogger : IAsyncDisposable
{
    /// <summary>
    /// Starts the background logging task.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stops the background logging task gracefully.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Queues a network request for logging.
    /// Non-blocking - returns immediately.
    /// </summary>
    Task LogRequestAsync(NetworkRequest request);

    /// <summary>
    /// Gets the most recent logged requests.
    /// </summary>
    /// <param name="count">Maximum number of entries to return.</param>
    Task<IEnumerable<NetworkRequest>> GetRecentRequestsAsync(int count);

    /// <summary>
    /// Gets logged requests matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="currentPageHost">Current page host for third-party detection.</param>
    Task<IEnumerable<NetworkRequest>> GetFilteredRequestsAsync(
        NetworkRequestFilter filter,
        string? currentPageHost = null);

    /// <summary>
    /// Gets statistics about logged requests.
    /// </summary>
    Task<NetworkLogStats> GetStatsAsync();

    /// <summary>
    /// Clears all logged requests.
    /// </summary>
    Task ClearAllAsync();
}

/// <summary>
/// Statistics about logged network requests.
/// </summary>
public record NetworkLogStats(
    int TotalRequests,
    int BlockedRequests,
    long TotalBytes,
    string FormattedDataSaved);
