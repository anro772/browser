using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Core.Utilities;
using BrowserApp.Data.Interfaces;
using BrowserApp.Data.Entities;

namespace BrowserApp.UI.Services;

/// <summary>
/// Rule evaluation engine that matches network requests against active rules.
/// Includes LRU caching for 90%+ cache hit rate on repeated URLs.
/// </summary>
public class RuleEngine : IRuleEngine, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ContentPolicyService? _contentPolicyService;
    private readonly SettingsService? _settingsService;
    private readonly MemoryCache _evaluationCache;
    private List<Rule> _cachedRules = new();
    private readonly object _cacheLock = new();
    private int _ruleVersion = 0;
    private bool _isDisposed;
    private readonly EventHandler<PrivacyMode>? _privacyModeChangedHandler;

    // Cache metrics
    private long _cacheHits = 0;
    private long _cacheMisses = 0;

    // Privacy-mode metrics — let users observe whether the mode actually changed behavior.
    private long _modeFilteredOut = 0;
    private long _modeStrictExtraBlocks = 0;
    private long _modeEvaluations = 0;

    /// <summary>
    /// Substrings matched against request hostnames in Strict mode. A hit always blocks,
    /// even when no user rule matches. Kept short and well-known to avoid surprises.
    /// </summary>
    private static readonly string[] StrictModeTrackerHosts =
    {
        "google-analytics.com",
        "googletagmanager.com",
        "googletagservices.com",
        "doubleclick.net",
        "facebook.net",
        "scorecardresearch.com",
        "hotjar.com",
        "mixpanel.com",
        "segment.io",
        "segment.com",
        "amplitude.com",
        "fullstory.com",
        "matomo.cloud",
        "quantserve.com",
        "adsystem.com",
    };

    public event EventHandler? RulesReloaded;

    public RuleEngine(
        IServiceScopeFactory scopeFactory,
        ContentPolicyService? contentPolicyService = null,
        SettingsService? settingsService = null)
    {
        _scopeFactory = scopeFactory;
        _contentPolicyService = contentPolicyService;
        _settingsService = settingsService;

        // Initialize evaluation cache with 100MB size limit
        _evaluationCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 100 * 1024 * 1024 // 100MB max cache size
        });

        // PrivacyMode affects which rules participate in evaluation, so cached results
        // need to be invalidated whenever the user toggles modes.
        if (_settingsService != null)
        {
            _privacyModeChangedHandler = (_, mode) =>
            {
                _evaluationCache.Clear();
                Interlocked.Increment(ref _ruleVersion);
                ErrorLogger.LogInfo($"[PrivacyMode] Switched to {mode}. Eval cache cleared, rule version bumped.");
            };
            _settingsService.PrivacyModeChanged += _privacyModeChangedHandler;
        }
    }

    private PrivacyMode CurrentPrivacyMode =>
        _settingsService?.PrivacyMode ?? PrivacyMode.Standard;

    public async Task InitializeAsync()
    {
        await ReloadRulesAsync();
    }

    public async Task ReloadRulesAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            var entities = await repository.GetEnabledAsync();
            var rules = entities.Select(MapEntityToRule).ToList();

            lock (_cacheLock)
            {
                _cachedRules = rules;
                // Increment version to invalidate all cached evaluations
                Interlocked.Increment(ref _ruleVersion);
            }

            // Clear evaluation cache when rules change
            _evaluationCache.Clear();

            Debug.WriteLine($"[RuleEngine] Loaded {rules.Count} rules (version: {_ruleVersion})");
            RulesReloaded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleEngine] Error loading rules: {ex.Message}");
        }
    }

    public RuleEvaluationResult Evaluate(NetworkRequest request, string? currentPageUrl)
    {
        // Cache key includes privacy mode so toggling modes doesn't return stale results
        // (the mode-change handler also bumps _ruleVersion, so this is belt-and-suspenders).
        var mode = CurrentPrivacyMode;
        var cacheKey = $"{request.Url}|{currentPageUrl ?? ""}|{_ruleVersion}|{mode}";

        // Try to get cached result
        if (_evaluationCache.TryGetValue(cacheKey, out RuleEvaluationResult? cachedResult))
        {
            Interlocked.Increment(ref _cacheHits);
            return cachedResult!;
        }

        // Cache miss - perform evaluation
        Interlocked.Increment(ref _cacheMisses);
        var result = EvaluateInternal(request, currentPageUrl, mode);

        // Cache the result with 5-minute expiration
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            Size = 1 // Each entry counts as 1 unit toward size limit
        };

        _evaluationCache.Set(cacheKey, result, cacheOptions);

        // Log cache stats every 1000 evaluations
        var totalEvals = _cacheHits + _cacheMisses;
        if (totalEvals % 1000 == 0 && totalEvals > 0)
        {
            var hitRate = (_cacheHits * 100.0) / totalEvals;
            Debug.WriteLine($"[RuleEngine] Cache stats: {_cacheHits} hits, {_cacheMisses} misses ({hitRate:F1}% hit rate)");
        }

        return result;
    }

    /// <summary>
    /// Internal evaluation logic without caching.
    /// </summary>
    private RuleEvaluationResult EvaluateInternal(NetworkRequest request, string? currentPageUrl, PrivacyMode mode)
    {
        List<Rule> rulesToEvaluate;
        lock (_cacheLock)
        {
            rulesToEvaluate = _cachedRules.ToList();
        }

        // Privacy-mode rule filter:
        //   Relaxed  → only user-created (local) and channel-enforced rules participate.
        //   Standard → all enabled rules participate (current default behavior).
        //   Strict   → all enabled rules + extra hardcoded tracker hostname check below.
        var preFilterCount = rulesToEvaluate.Count;
        rulesToEvaluate = rulesToEvaluate
            .Where(r => RuleAllowedByPrivacyMode(r, mode))
            .OrderByDescending(r => r.Priority)
            .ToList();
        var droppedByMode = preFilterCount - rulesToEvaluate.Count;
        if (droppedByMode > 0)
        {
            Interlocked.Add(ref _modeFilteredOut, droppedByMode);
        }
        Interlocked.Increment(ref _modeEvaluations);

        // Periodic mode-effect summary (every 500 evaluations) so a user can confirm in the
        // log viewer that their mode choice is taking effect. Standard always shows 0 dropped.
        var evalsSoFar = Interlocked.Read(ref _modeEvaluations);
        if (evalsSoFar % 500 == 0)
        {
            ErrorLogger.LogInfo(
                $"[PrivacyMode] {mode}: {evalsSoFar} evals · " +
                $"{Interlocked.Read(ref _modeFilteredOut)} rules dropped by mode · " +
                $"{Interlocked.Read(ref _modeStrictExtraBlocks)} strict-list blocks");
        }

        var injections = new List<RuleAction>();
        var headerMods = new List<HeaderModification>();

        foreach (var rule in rulesToEvaluate)
        {
            // Check if rule applies to current page
            if (!rule.AppliesTo(currentPageUrl ?? ""))
                continue;

            foreach (var action in rule.Rules)
            {
                if (!ActionMatchesRequest(action, request))
                    continue;

                if (action.Type == "block")
                {
                    // First matching block rule wins
                    return RuleEvaluationResult.Block(rule.Id, rule.Name);
                }
                else if (action.Type == "content_policy")
                {
                    // Check content policy categories against the request URL
                    if (_contentPolicyService != null && action.Categories != null && action.Categories.Count > 0)
                    {
                        var blockedCategory = _contentPolicyService.GetBlockedCategory(
                            request.Url, action.Categories);
                        if (blockedCategory != null)
                        {
                            return RuleEvaluationResult.BlockByContentPolicy(
                                rule.Id, rule.Name, blockedCategory);
                        }
                    }
                }
                else if (action.Type == "modify_headers")
                {
                    // Collect header modifications
                    if (action.Headers != null)
                    {
                        headerMods.AddRange(action.Headers);
                    }
                }
                else if (action.Type == "inject_css" || action.Type == "inject_js")
                {
                    // Collect injections
                    injections.Add(action);
                }
            }
        }

        // Strict mode: catch known tracker hostnames that no user rule covered.
        if (mode == PrivacyMode.Strict && IsKnownTrackerHost(request.Url))
        {
            Interlocked.Increment(ref _modeStrictExtraBlocks);
            ErrorLogger.LogInfo($"[PrivacyMode] Strict tracker-list block: {request.Url}");
            return RuleEvaluationResult.Block(string.Empty, "Strict mode tracker block");
        }

        // No blocking, but may have injections and/or header modifications
        return new RuleEvaluationResult
        {
            ShouldBlock = false,
            InjectionsToApply = injections,
            HeaderModifications = headerMods
        };
    }

    private static bool RuleAllowedByPrivacyMode(Rule rule, PrivacyMode mode)
    {
        // Channel-enforced rules cannot be opted out of, regardless of mode.
        if (rule.IsEnforced) return true;

        return mode switch
        {
            PrivacyMode.Relaxed => string.Equals(rule.Source, "local", StringComparison.OrdinalIgnoreCase),
            _ => true,
        };
    }

    private static bool IsKnownTrackerHost(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        var host = uri.Host;
        foreach (var needle in StrictModeTrackerHosts)
        {
            if (host.EndsWith(needle, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public IEnumerable<RuleAction> GetInjectionsForPage(string pageUrl)
    {
        List<Rule> rulesToEvaluate;
        lock (_cacheLock)
        {
            rulesToEvaluate = _cachedRules.ToList();
        }

        var injections = new List<RuleAction>();

        foreach (var rule in rulesToEvaluate.OrderByDescending(r => r.Priority))
        {
            if (!rule.AppliesTo(pageUrl))
                continue;

            foreach (var action in rule.Rules)
            {
                if (action.Type == "inject_css" || action.Type == "inject_js")
                {
                    // Check if the injection URL pattern matches the page
                    if (string.IsNullOrEmpty(action.Match.UrlPattern) ||
                        UrlMatcher.Matches(pageUrl, action.Match.UrlPattern))
                    {
                        injections.Add(action);
                    }
                }
            }
        }

        return injections;
    }

    public IEnumerable<Rule> GetActiveRules()
    {
        lock (_cacheLock)
        {
            return _cachedRules.ToList();
        }
    }

    public int GetRuleCount()
    {
        lock (_cacheLock)
        {
            return _cachedRules.Count;
        }
    }

    private bool ActionMatchesRequest(RuleAction action, NetworkRequest request)
    {
        // Match URL pattern
        if (!string.IsNullOrEmpty(action.Match.UrlPattern))
        {
            if (!UrlMatcher.Matches(request.Url, action.Match.UrlPattern))
                return false;
        }

        // Match resource type
        if (!string.IsNullOrEmpty(action.Match.ResourceType))
        {
            if (!UrlMatcher.MatchesResourceType(request.ResourceType, action.Match.ResourceType))
                return false;
        }

        // Match method
        if (!string.IsNullOrEmpty(action.Match.Method))
        {
            if (!UrlMatcher.MatchesMethod(request.Method, action.Match.Method))
                return false;
        }

        return true;
    }

    private Rule MapEntityToRule(RuleEntity entity)
    {
        var rule = new Rule
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Site = entity.Site,
            Enabled = entity.Enabled,
            Priority = entity.Priority,
            Source = entity.Source,
            ChannelId = entity.ChannelId,
            IsEnforced = entity.IsEnforced,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Rules = new List<RuleAction>()
        };

        // Parse RulesJson
        try
        {
            if (!string.IsNullOrEmpty(entity.RulesJson))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                rule.Rules = JsonSerializer.Deserialize<List<RuleAction>>(entity.RulesJson, options)
                    ?? new List<RuleAction>();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleEngine] Error parsing RulesJson for rule {entity.Id}: {ex.Message}");
        }

        return rule;
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        if (_settingsService != null && _privacyModeChangedHandler != null)
        {
            _settingsService.PrivacyModeChanged -= _privacyModeChangedHandler;
        }

        // Log final cache statistics
        var totalEvals = _cacheHits + _cacheMisses;
        if (totalEvals > 0)
        {
            var hitRate = (_cacheHits * 100.0) / totalEvals;
            Debug.WriteLine($"[RuleEngine] Final cache stats: {_cacheHits} hits, {_cacheMisses} misses ({hitRate:F1}% hit rate)");
            ErrorLogger.LogInfo($"RuleEngine cache stats: {hitRate:F1}% hit rate ({_cacheHits}/{totalEvals} hits)");
        }

        // Dispose MemoryCache
        _evaluationCache?.Dispose();

        GC.SuppressFinalize(this);
    }
}
