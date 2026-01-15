namespace BrowserApp.Core.AdBlocker.Interfaces;

/// <summary>
/// Service for blocking ads and trackers using filter lists.
/// </summary>
public interface IAdBlockerService
{
    /// <summary>
    /// Initializes the ad blocker by loading filter lists.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Checks if a URL should be blocked based on loaded filters.
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <param name="resourceType">The resource type (script, image, etc.)</param>
    /// <param name="pageUrl">The URL of the page making the request (for third-party checks)</param>
    /// <returns>True if the URL should be blocked, false otherwise</returns>
    bool ShouldBlock(string url, string? resourceType = null, string? pageUrl = null);

    /// <summary>
    /// Gets statistics about the loaded filters.
    /// </summary>
    AdBlockerStats GetStats();

    /// <summary>
    /// Reloads filter lists from disk or downloads new ones.
    /// </summary>
    Task ReloadFiltersAsync();
}

/// <summary>
/// Statistics about loaded filters and blocking performance.
/// </summary>
public class AdBlockerStats
{
    public int TotalFilters { get; set; }
    public int NetworkFilters { get; set; }
    public int CosmeticFilters { get; set; }
    public long TotalChecks { get; set; }
    public long TotalBlocks { get; set; }
    public double AverageCheckTimeMs { get; set; }
}
