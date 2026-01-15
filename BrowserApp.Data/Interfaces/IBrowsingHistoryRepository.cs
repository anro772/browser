using BrowserApp.Data.Entities;

namespace BrowserApp.Data.Interfaces;

/// <summary>
/// Repository interface for browsing history operations.
/// </summary>
public interface IBrowsingHistoryRepository
{
    /// <summary>
    /// Adds a new history entry.
    /// </summary>
    Task AddAsync(BrowsingHistoryEntity history);

    /// <summary>
    /// Gets the most recent history entries.
    /// </summary>
    /// <param name="count">Maximum number of entries to return.</param>
    Task<IEnumerable<BrowsingHistoryEntity>> GetRecentAsync(int count);

    /// <summary>
    /// Searches history by URL or title.
    /// </summary>
    /// <param name="query">Search query string.</param>
    Task<IEnumerable<BrowsingHistoryEntity>> SearchAsync(string query);
}
