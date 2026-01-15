namespace BrowserApp.Core.AdBlocker.Interfaces;

/// <summary>
/// Downloads filter lists from remote sources.
/// </summary>
public interface IFilterListDownloader
{
    /// <summary>
    /// Downloads a filter list from a URL.
    /// </summary>
    /// <param name="url">The URL of the filter list</param>
    /// <returns>The raw filter list content</returns>
    Task<string> DownloadFilterListAsync(string url);

    /// <summary>
    /// Downloads multiple filter lists concurrently.
    /// </summary>
    /// <param name="urls">URLs of the filter lists</param>
    /// <returns>Dictionary mapping URL to content</returns>
    Task<Dictionary<string, string>> DownloadMultipleAsync(IEnumerable<string> urls);
}
