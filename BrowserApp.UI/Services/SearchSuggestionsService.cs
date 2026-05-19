using System.Net.Http;
using System.Text.Json;

namespace BrowserApp.UI.Services;

/// <summary>
/// Fetches autocomplete suggestions from Google's public complete endpoint.
/// No API key required. Used by <see cref="ViewModels.MainViewModel"/> to enrich the
/// address-bar autocomplete popup alongside local history and bookmarks.
/// </summary>
public class SearchSuggestionsService
{
    // Same endpoint Firefox uses — returns plain JSON ["query", ["s1","s2",...]].
    private const string EndpointFormat = "https://www.google.com/complete/search?client=firefox&hl=en&q={0}";

    private readonly HttpClient _http;

    public SearchSuggestionsService()
    {
        _http = new HttpClient(new HttpClientHandler { UseCookies = false })
        {
            Timeout = TimeSpan.FromSeconds(3),
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 BrowserApp");
    }

    public async Task<IReadOnlyList<string>> GetAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<string>();

        try
        {
            var url = string.Format(EndpointFormat, Uri.EscapeDataString(query));
            var json = await _http.GetStringAsync(url, ct);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 2)
                return Array.Empty<string>();

            var arr = root[1];
            if (arr.ValueKind != JsonValueKind.Array) return Array.Empty<string>();

            var list = new List<string>(arr.GetArrayLength());
            foreach (var item in arr.EnumerateArray())
            {
                var s = item.GetString();
                if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
            }
            return list;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Offline / timeout / parse error — degrade gracefully to local-only suggestions.
            return Array.Empty<string>();
        }
    }
}
