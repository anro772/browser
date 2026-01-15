using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Core.Utilities;
using BrowserApp.Data.Interfaces;
using BrowserApp.Data.Entities;

namespace BrowserApp.UI.Services;

/// <summary>
/// Rule evaluation engine that matches network requests against active rules.
/// </summary>
public class RuleEngine : IRuleEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private List<Rule> _cachedRules = new();
    private readonly object _cacheLock = new();
    private bool _isInitialized;

    public event EventHandler? RulesReloaded;

    public RuleEngine(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await ReloadRulesAsync();
        _isInitialized = true;
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
            }

            Debug.WriteLine($"[RuleEngine] Loaded {rules.Count} rules");
            RulesReloaded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleEngine] Error loading rules: {ex.Message}");
        }
    }

    public RuleEvaluationResult Evaluate(NetworkRequest request, string? currentPageUrl)
    {
        List<Rule> rulesToEvaluate;
        lock (_cacheLock)
        {
            rulesToEvaluate = _cachedRules.ToList();
        }

        // Sort by priority (higher first)
        rulesToEvaluate = rulesToEvaluate
            .OrderByDescending(r => r.Priority)
            .ToList();

        var injections = new List<RuleAction>();

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
                else if (action.Type == "inject_css" || action.Type == "inject_js")
                {
                    // Collect injections
                    injections.Add(action);
                }
            }
        }

        // No blocking, but may have injections
        return new RuleEvaluationResult
        {
            ShouldBlock = false,
            InjectionsToApply = injections
        };
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
}
