using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;

namespace BrowserApp.UI.Services;

public class FilterListService : IFilterListService, IDisposable
{
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrowserApp", "FilterLists");

    private readonly HttpClient _httpClient;
    private readonly List<FilterList> _lists;
    private readonly Timer _updateTimer;

    // Parsed filter data
    private readonly HashSet<string> _blockDomains = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _whitelistDomains = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Regex> _blockPatterns = new();
    private readonly List<Regex> _whitelistPatterns = new();
    private readonly ConcurrentDictionary<string, List<string>> _cosmeticSelectors = new();
    private readonly List<string> _globalCosmeticSelectors = new();
    private readonly object _lock = new();
    private volatile bool _isLoaded;

    public FilterListService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BrowserApp/1.0");

        _lists = new List<FilterList>
        {
            new()
            {
                Id = "easylist",
                Name = "EasyList",
                Url = "https://easylist.to/easylist/easylist.txt",
                Enabled = true
            },
            new()
            {
                Id = "easyprivacy",
                Name = "EasyPrivacy",
                Url = "https://easylist.to/easylist/easyprivacy.txt",
                Enabled = true
            }
        };

        // Auto-update every 24 hours
        _updateTimer = new Timer(_ => _ = UpdateAllAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(CacheDir);

        foreach (var list in _lists.Where(l => l.Enabled))
        {
            var cachePath = GetCachePath(list.Id);
            if (!File.Exists(cachePath))
            {
                await DownloadListAsync(list);
            }
            else
            {
                list.LastUpdated = File.GetLastWriteTimeUtc(cachePath);
            }

            LoadListFromCache(list);
        }

        _isLoaded = true;

        var totalFilters = GetTotalFilterCount();
        ErrorLogger.LogInfo($"FilterListService initialized: {totalFilters} filters from {_lists.Count(l => l.Enabled)} lists");

        // Start auto-update timer (24 hours)
        _updateTimer.Change(TimeSpan.FromHours(24), TimeSpan.FromHours(24));
    }

    public async Task UpdateAllAsync()
    {
        foreach (var list in _lists.Where(l => l.Enabled))
        {
            try
            {
                await DownloadListAsync(list);
                LoadListFromCache(list);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"Failed to update filter list: {list.Name}", ex);
            }
        }

        ErrorLogger.LogInfo($"Filter lists updated: {GetTotalFilterCount()} total filters");
    }

    public bool ShouldBlock(string requestUrl, string? pageUrl, string resourceType)
    {
        if (!_isLoaded || string.IsNullOrEmpty(requestUrl))
            return false;

        try
        {
            // Extract domain from request URL
            if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out var requestUri))
                return false;

            var requestDomain = requestUri.Host.ToLowerInvariant();

            // Check whitelist first
            if (IsDomainWhitelisted(requestDomain))
                return false;

            // Check domain block list (fast path)
            if (IsDomainBlocked(requestDomain))
                return true;

            // Check URL patterns (slower path, only if domain check missed)
            lock (_lock)
            {
                foreach (var pattern in _whitelistPatterns)
                {
                    if (pattern.IsMatch(requestUrl))
                        return false;
                }

                foreach (var pattern in _blockPatterns)
                {
                    if (pattern.IsMatch(requestUrl))
                        return true;
                }
            }
        }
        catch
        {
            // Fail open — don't block if evaluation fails
        }

        return false;
    }

    public string? GetCosmeticCss(string pageUrl)
    {
        if (!_isLoaded || string.IsNullOrEmpty(pageUrl))
            return null;

        try
        {
            if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri))
                return null;

            var domain = uri.Host.ToLowerInvariant();
            var selectors = new List<string>();

            // Add global cosmetic selectors
            selectors.AddRange(_globalCosmeticSelectors);

            // Add domain-specific selectors
            if (_cosmeticSelectors.TryGetValue(domain, out var domainSelectors))
                selectors.AddRange(domainSelectors);

            // Also check parent domains (e.g., "example.com" for "www.example.com")
            var parts = domain.Split('.');
            for (int i = 1; i < parts.Length - 1; i++)
            {
                var parentDomain = string.Join('.', parts.Skip(i));
                if (_cosmeticSelectors.TryGetValue(parentDomain, out var parentSelectors))
                    selectors.AddRange(parentSelectors);
            }

            if (selectors.Count == 0)
                return null;

            // Build CSS rule
            return string.Join(", ", selectors.Distinct()) + " { display: none !important; }";
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<FilterList> GetLists() => _lists.AsReadOnly();

    public async Task SetListEnabledAsync(string listId, bool enabled)
    {
        var list = _lists.FirstOrDefault(l => l.Id == listId);
        if (list == null) return;

        list.Enabled = enabled;

        if (enabled)
        {
            var cachePath = GetCachePath(list.Id);
            if (!File.Exists(cachePath))
                await DownloadListAsync(list);
            LoadListFromCache(list);
        }
        else
        {
            // Rebuild filters without this list
            ClearParsedData();
            foreach (var l in _lists.Where(x => x.Enabled))
                LoadListFromCache(l);
        }
    }

    public int GetTotalFilterCount()
    {
        lock (_lock)
        {
            return _blockDomains.Count + _blockPatterns.Count +
                   _globalCosmeticSelectors.Count +
                   _cosmeticSelectors.Values.Sum(v => v.Count);
        }
    }

    private async Task DownloadListAsync(FilterList list)
    {
        try
        {
            var content = await _httpClient.GetStringAsync(list.Url);
            var cachePath = GetCachePath(list.Id);
            await File.WriteAllTextAsync(cachePath, content);
            list.LastUpdated = DateTime.UtcNow;
            ErrorLogger.LogInfo($"Downloaded filter list: {list.Name} ({content.Length / 1024}KB)");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to download filter list: {list.Name}", ex);
        }
    }

    private void LoadListFromCache(FilterList list)
    {
        var cachePath = GetCachePath(list.Id);
        if (!File.Exists(cachePath)) return;

        try
        {
            var lines = File.ReadAllLines(cachePath);
            int parsed = 0;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // Skip comments and metadata
                if (string.IsNullOrEmpty(line) || line.StartsWith("!") || line.StartsWith("["))
                    continue;

                if (ParseFilterLine(line))
                    parsed++;
            }

            list.FilterCount = parsed;
            ErrorLogger.LogInfo($"Loaded filter list: {list.Name} — {parsed} filters parsed");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to load filter list from cache: {list.Name}", ex);
        }
    }

    private bool ParseFilterLine(string line)
    {
        // Cosmetic filter: domain##selector or ##selector
        var cosmeticIdx = line.IndexOf("##");
        if (cosmeticIdx >= 0)
        {
            var domains = cosmeticIdx > 0 ? line.Substring(0, cosmeticIdx) : "";
            var selector = line.Substring(cosmeticIdx + 2).Trim();

            if (string.IsNullOrEmpty(selector)) return false;

            // Skip procedural cosmetic filters (complex selectors we can't handle)
            if (selector.Contains(":has(") || selector.Contains(":style(") ||
                selector.Contains(":remove(") || selector.Contains(":matches-path("))
                return false;

            if (string.IsNullOrEmpty(domains))
            {
                // Global cosmetic selector
                lock (_lock)
                    _globalCosmeticSelectors.Add(selector);
            }
            else
            {
                // Domain-specific cosmetic selector
                foreach (var domain in domains.Split(','))
                {
                    var d = domain.Trim().TrimStart('~');
                    if (!string.IsNullOrEmpty(d) && !domain.StartsWith("~"))
                    {
                        _cosmeticSelectors.GetOrAdd(d.ToLowerInvariant(), _ => new List<string>())
                            .Add(selector);
                    }
                }
            }
            return true;
        }

        // Exception/whitelist rule: @@||domain.com^
        if (line.StartsWith("@@"))
        {
            var pattern = line.Substring(2);
            if (pattern.StartsWith("||") && pattern.EndsWith("^"))
            {
                var domain = pattern.Substring(2, pattern.Length - 3);
                if (!domain.Contains('*') && !domain.Contains('/'))
                {
                    lock (_lock)
                        _whitelistDomains.Add(domain);
                    return true;
                }
            }
            // Complex whitelist pattern — skip for now
            return false;
        }

        // Domain block: ||domain.com^
        if (line.StartsWith("||"))
        {
            var rest = line.Substring(2);

            // Remove modifiers after $
            var modIdx = rest.IndexOf('$');
            string? modifiers = null;
            if (modIdx >= 0)
            {
                modifiers = rest.Substring(modIdx + 1);
                rest = rest.Substring(0, modIdx);
            }

            // Skip rules with unsupported modifiers
            if (modifiers != null)
            {
                var mods = modifiers.Split(',');
                if (mods.Any(m => m.StartsWith("redirect") || m.StartsWith("csp") ||
                                  m.StartsWith("rewrite") || m == "popup" || m == "popunder"))
                    return false;
            }

            // Simple domain block: ||domain.com^
            if (rest.EndsWith("^"))
            {
                var domain = rest.Substring(0, rest.Length - 1);
                if (!domain.Contains('*') && !domain.Contains('/'))
                {
                    lock (_lock)
                        _blockDomains.Add(domain);
                    return true;
                }
            }

            // URL path pattern: ||domain.com/path
            if (!rest.Contains('*'))
            {
                // Simple prefix match
                var escaped = Regex.Escape(rest.TrimEnd('^'));
                try
                {
                    var regex = new Regex(escaped, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    lock (_lock)
                        _blockPatterns.Add(regex);
                    return true;
                }
                catch { return false; }
            }

            return false;
        }

        // Generic URL pattern with wildcards
        if (line.Contains('*') && !line.Contains('#') && !line.StartsWith('/'))
        {
            // Skip overly broad patterns
            if (line == "*" || line.Length < 5) return false;

            try
            {
                var escaped = Regex.Escape(line.TrimEnd('^'));
                escaped = escaped.Replace("\\*", ".*");
                var regex = new Regex(escaped, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                lock (_lock)
                    _blockPatterns.Add(regex);
                return true;
            }
            catch { return false; }
        }

        return false;
    }

    private bool IsDomainBlocked(string domain)
    {
        lock (_lock)
        {
            // Exact match
            if (_blockDomains.Contains(domain))
                return true;

            // Check parent domains (e.g., block "ads.example.com" if "example.com" is blocked)
            var parts = domain.Split('.');
            for (int i = 1; i < parts.Length - 1; i++)
            {
                var parent = string.Join('.', parts.Skip(i));
                if (_blockDomains.Contains(parent))
                    return true;
            }
        }
        return false;
    }

    private bool IsDomainWhitelisted(string domain)
    {
        lock (_lock)
        {
            if (_whitelistDomains.Contains(domain))
                return true;

            var parts = domain.Split('.');
            for (int i = 1; i < parts.Length - 1; i++)
            {
                var parent = string.Join('.', parts.Skip(i));
                if (_whitelistDomains.Contains(parent))
                    return true;
            }
        }
        return false;
    }

    private void ClearParsedData()
    {
        lock (_lock)
        {
            _blockDomains.Clear();
            _whitelistDomains.Clear();
            _blockPatterns.Clear();
            _whitelistPatterns.Clear();
            _globalCosmeticSelectors.Clear();
            _cosmeticSelectors.Clear();
        }
    }

    private static string GetCachePath(string listId) =>
        Path.Combine(CacheDir, $"{listId}.txt");

    public void Dispose()
    {
        _updateTimer.Dispose();
        _httpClient.Dispose();
    }
}
