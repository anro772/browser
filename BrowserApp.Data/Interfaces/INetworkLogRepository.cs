using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;

namespace BrowserApp.Data.Interfaces;

/// <summary>
/// Repository interface for network log operations.
/// </summary>
public interface INetworkLogRepository
{
    /// <summary>
    /// Adds a new network log entry.
    /// </summary>
    Task AddAsync(NetworkLogEntity log);

    /// <summary>
    /// Adds multiple network log entries in a single batch.
    /// More efficient than individual inserts for high-volume logging.
    /// </summary>
    Task AddBatchAsync(IEnumerable<NetworkLogEntity> logs);

    /// <summary>
    /// Gets the most recent network log entries.
    /// </summary>
    /// <param name="count">Maximum number of entries to return.</param>
    Task<IEnumerable<NetworkLogEntity>> GetRecentAsync(int count);

    /// <summary>
    /// Gets network log entries matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="currentPageHost">Current page host for third-party detection.</param>
    /// <param name="count">Maximum number of entries to return.</param>
    Task<IEnumerable<NetworkLogEntity>> GetByFilterAsync(
        NetworkRequestFilter filter,
        string? currentPageHost = null,
        int count = 1000);

    /// <summary>
    /// Gets the total count of logged requests.
    /// </summary>
    Task<int> GetCountAsync();

    /// <summary>
    /// Gets the count of blocked requests.
    /// </summary>
    Task<int> GetBlockedCountAsync();

    /// <summary>
    /// Gets the total size of all logged requests in bytes.
    /// </summary>
    Task<long> GetTotalSizeAsync();

    /// <summary>
    /// Clears all network log entries.
    /// </summary>
    Task ClearAllAsync();
}
