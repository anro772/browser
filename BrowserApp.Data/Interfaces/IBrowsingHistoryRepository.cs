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

    /// <summary>
    /// Clears all history entries.
    /// </summary>
    Task ClearAllAsync();

    /// <summary>
    /// Gets the most frequently visited sites (by visit count), grouped by domain.
    /// </summary>
    /// <param name="count">Maximum number of sites to return.</param>
    Task<IEnumerable<FrequentSite>> GetFrequentSitesAsync(int count);
}

/// <summary>
/// Represents a frequently visited site for the new tab page.
/// </summary>
public class FrequentSite
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int VisitCount { get; set; }
}
