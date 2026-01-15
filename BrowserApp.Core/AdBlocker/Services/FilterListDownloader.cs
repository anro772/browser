using System.Diagnostics;
using BrowserApp.Core.AdBlocker.Interfaces;

namespace BrowserApp.Core.AdBlocker.Services;

/// <summary>
/// Downloads filter lists from remote URLs.
/// Includes retry logic and caching.
/// </summary>
public class FilterListDownloader : IFilterListDownloader
{
    private readonly HttpClient _httpClient;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    public FilterListDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = RequestTimeout;
    }

    public async Task<string> DownloadFilterListAsync(string url)
    {
        var retries = 3;
        Exception? lastException = null;

        for (int i = 0; i < retries; i++)
        {
            try
            {
                Debug.WriteLine($"[FilterListDownloader] Downloading: {url} (attempt {i + 1}/{retries})");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[FilterListDownloader] Downloaded {content.Length} chars from {url}");

                return content;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Debug.WriteLine($"[FilterListDownloader] Error downloading {url}: {ex.Message}");

                if (i < retries - 1)
                {
                    // Wait before retry (exponential backoff)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
                }
            }
        }

        throw new InvalidOperationException(
            $"Failed to download filter list from {url} after {retries} attempts",
            lastException);
    }

    public async Task<Dictionary<string, string>> DownloadMultipleAsync(IEnumerable<string> urls)
    {
        var tasks = urls.Select(async url =>
        {
            try
            {
                var content = await DownloadFilterListAsync(url);
                return new KeyValuePair<string, string>(url, content);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FilterListDownloader] Failed to download {url}: {ex.Message}");
                return new KeyValuePair<string, string>(url, string.Empty);
            }
        });

        var results = await Task.WhenAll(tasks);

        return results
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
