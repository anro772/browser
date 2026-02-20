using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.DTOs.Ollama;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.UI.Services;

public class RuleGenerationService : IRuleGenerationService
{
    private readonly IOllamaClient _ollamaClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuleEngine _ruleEngine;

    private const string SystemPrompt = @"You generate ad-blocking rules as JSON. Respond with ONLY a JSON array, no other text.

Output this exact structure with the site filled in:
{""rules"":[
{""Name"":""Block Ad Networks"",""Site"":""SITE_HERE"",""Priority"":50,""Rules"":[
{""Type"":""block"",""Match"":{""UrlPattern"":""*doubleclick.net*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*googlesyndication.com*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*google-analytics.com*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*googletagmanager.com*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*googleadservices.com*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*facebook.net*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*adnxs.com*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*amazon-adsystem.com*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*taboola.com*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*outbrain.com*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*criteo.com*""},""Css"":null,""Js"":null},
{""Type"":""block"",""Match"":{""UrlPattern"":""*pubmatic.com*""},""Css"":null,""Js"":null}
]},
{""Name"":""Hide Ad Elements"",""Site"":""SITE_HERE"",""Priority"":40,""Rules"":[
{""Type"":""inject_css"",""Match"":{""UrlPattern"":""*""},""Css"":"".ad-container,.ad-banner,.ad-wrapper,[class*=ad-],[id*=ad-],.sponsored,.advertisement,[class*=popup],[class*=overlay],.interstitial{display:none!important}"",""Js"":null}
]},
{""Name"":""Remove Popups"",""Site"":""SITE_HERE"",""Priority"":30,""Rules"":[
{""Type"":""inject_js"",""Match"":{""UrlPattern"":""*""},""Css"":null,""Js"":""document.body.style.overflow='auto';setInterval(function(){document.querySelectorAll('[class*=popup],[class*=overlay],[class*=modal]').forEach(function(e){if(getComputedStyle(e).position==='fixed')e.remove()})},2000)""}
]}
]}

Replace SITE_HERE with the actual domain pattern. Add MORE site-specific block domains. NEVER block the site's own domain.";

    public RuleGenerationService(
        IOllamaClient ollamaClient,
        IServiceScopeFactory scopeFactory,
        IRuleEngine ruleEngine)
    {
        _ollamaClient = ollamaClient;
        _scopeFactory = scopeFactory;
        _ruleEngine = ruleEngine;
    }

    public async Task<List<Rule>> GenerateRuleSuggestionsAsync(string url, string? title = null)
    {
        var domain = string.Empty;
        try { domain = new Uri(url).Host.ToLowerInvariant(); } catch { }

        if (string.IsNullOrEmpty(domain))
            return new List<Rule>();

        // Build reliable rules programmatically with standard ad-blocking patterns
        var sitePattern = $"*.{domain}";
        var rules = new List<Rule>();

        // Rule 1: Block common ad/tracker network domains
        var blockRule = new Rule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Block Ad Networks on {domain}",
            Description = "Block third-party ad and tracker network requests",
            Site = sitePattern,
            Priority = 50,
            Source = "ai",
            Rules = GetStandardBlockActions()
        };
        rules.Add(blockRule);

        // Rule 2: CSS cosmetic filtering - hide ad elements
        var cssRule = new Rule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Hide Ad Elements on {domain}",
            Description = "Hide ad containers, banners, and overlays with CSS",
            Site = sitePattern,
            Priority = 40,
            Source = "ai",
            Rules = new List<RuleAction>
            {
                new()
                {
                    Type = "inject_css",
                    Match = new RuleMatch { UrlPattern = "*" },
                    Css = GetStandardAdHidingCss(),
                    Timing = "dom_ready"
                }
            }
        };
        rules.Add(cssRule);

        // Rule 3: JS popup/overlay removal
        var jsRule = new Rule
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Remove Popups on {domain}",
            Description = "Remove popup overlays and restore scrolling",
            Site = sitePattern,
            Priority = 30,
            Source = "ai",
            Rules = new List<RuleAction>
            {
                new()
                {
                    Type = "inject_js",
                    Match = new RuleMatch { UrlPattern = "*" },
                    Js = GetStandardPopupRemovalJs(),
                    Timing = "dom_ready"
                }
            }
        };
        rules.Add(jsRule);

        // Try to get AI-suggested additional domains to block
        try
        {
            var aiDomains = await GetAiSuggestedBlockDomains(url, domain, title);
            if (aiDomains.Count > 0)
            {
                var aiBlockRule = new Rule
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Block Site-Specific Ads on {domain}",
                    Description = "Block site-specific ad domains suggested by AI",
                    Site = sitePattern,
                    Priority = 45,
                    Source = "ai",
                    Rules = aiDomains.Select(d => new RuleAction
                    {
                        Type = "block",
                        Match = new RuleMatch { UrlPattern = $"*{d}*" }
                    }).ToList()
                };
                rules.Add(aiBlockRule);
            }
        }
        catch (Exception ex)
        {
            // AI enhancement is optional - standard rules still work
            ErrorLogger.LogInfo($"AI domain suggestions unavailable: {ex.Message}");
        }

        return rules;
    }

    private async Task<List<string>> GetAiSuggestedBlockDomains(string url, string domain, string? title)
    {
        var messages = new List<OllamaChatMessage>
        {
            new() { Role = "system", Content = "List ad/tracker domains for a website as JSON: {\"domains\":[\"ad.example.com\",\"tracker.example.com\"]}. Only list third-party domains, NOT the site itself. Respond with JSON only." },
            new() { Role = "user", Content = $"List ad domains for {domain}" + (title != null ? $" ({title})" : "") }
        };

        var response = await _ollamaClient.ChatJsonAsync(messages);
        if (string.IsNullOrEmpty(response)) return new List<string>();

        try
        {
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    return prop.Value.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!)
                        .Where(d => !string.IsNullOrWhiteSpace(d) && !d.Contains(domain)) // Filter out the site's own domain
                        .Take(10)
                        .ToList();
                }
            }
        }
        catch { }

        return new List<string>();
    }

    private static List<RuleAction> GetStandardBlockActions()
    {
        var adDomains = new[]
        {
            "doubleclick.net", "googlesyndication.com", "google-analytics.com",
            "googletagmanager.com", "googleadservices.com", "pagead2.googlesyndication.com",
            "adnxs.com", "amazon-adsystem.com", "taboola.com", "outbrain.com",
            "criteo.com", "pubmatic.com", "openx.net", "rubiconproject.com",
            "facebook.net/tr", "facebook.com/tr", "connect.facebook.net",
            "hotjar.com", "mixpanel.com", "segment.io",
            "popads.net", "popcash.net", "propellerads.com",
            "exoclick.com", "juicyads.com", "trafficjunky.net",
        };

        return adDomains.Select(d => new RuleAction
        {
            Type = "block",
            Match = new RuleMatch { UrlPattern = $"*{d}*" }
        }).ToList();
    }

    private static string GetStandardAdHidingCss()
    {
        return string.Join(", ", new[]
        {
            ".ad-container", ".ad-banner", ".ad-wrapper", ".ad-block",
            "[class*=\"ad-\"]", "[class*=\"ad_\"]", "[id*=\"ad-\"]", "[id*=\"ad_\"]",
            ".advertisement", ".sponsored", ".ad-slot",
            "[class*=\"popup\"]", "[class*=\"overlay\"]",
            ".interstitial", ".modal-overlay",
            "[class*=\"banner-ad\"]", "[id*=\"banner-ad\"]",
            ".adsbygoogle", "[id*=\"google_ads\"]",
            "iframe[src*=\"ads\"]", "iframe[src*=\"doubleclick\"]",
        }) + " { display: none !important; }";
    }

    private static string GetStandardPopupRemovalJs()
    {
        return @"(function(){
            document.body.style.overflow='auto';
            document.documentElement.style.overflow='auto';
            setInterval(function(){
                document.querySelectorAll('[class*=""popup""],[class*=""overlay""],[class*=""modal""],[class*=""interstitial""]').forEach(function(el){
                    var s=getComputedStyle(el);
                    if(s.position==='fixed'||s.position==='absolute'){
                        var r=el.getBoundingClientRect();
                        if(r.width>window.innerWidth*0.5&&r.height>window.innerHeight*0.3){
                            el.remove();
                        }
                    }
                });
                document.body.style.overflow='auto';
                document.documentElement.style.overflow='auto';
            },3000);
        })();";
    }

    public async Task ApplyRuleAsync(Rule rule)
    {
        try
        {
            rule.Source = "ai";
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;

            using var scope = _scopeFactory.CreateScope();
            var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            // Skip if already applied (prevent duplicate on double-click)
            if (await ruleRepo.ExistsAsync(rule.Id))
                return;

            var entity = new RuleEntity
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                Site = rule.Site,
                Enabled = true,
                Priority = rule.Priority,
                RulesJson = JsonSerializer.Serialize(rule.Rules),
                Source = "ai",
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };

            await ruleRepo.AddAsync(entity);
            await _ruleEngine.ReloadRulesAsync();

            ErrorLogger.LogInfo($"Applied AI-generated rule: {rule.Name}");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to apply AI rule: {rule.Name}", ex);
            throw;
        }
    }

    public async Task ApplyAllRulesAsync(List<Rule> rules)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            foreach (var rule in rules)
            {
                rule.Source = "ai";
                rule.CreatedAt = DateTime.UtcNow;
                rule.UpdatedAt = DateTime.UtcNow;

                if (string.IsNullOrEmpty(rule.Id))
                    rule.Id = Guid.NewGuid().ToString();

                // Skip duplicates
                if (await ruleRepo.ExistsAsync(rule.Id))
                    continue;

                var entity = new RuleEntity
                {
                    Id = rule.Id,
                    Name = rule.Name,
                    Description = rule.Description,
                    Site = rule.Site,
                    Enabled = true,
                    Priority = rule.Priority,
                    RulesJson = JsonSerializer.Serialize(rule.Rules),
                    Source = "ai",
                    CreatedAt = rule.CreatedAt,
                    UpdatedAt = rule.UpdatedAt
                };

                await ruleRepo.AddAsync(entity);
            }

            await _ruleEngine.ReloadRulesAsync();

            ErrorLogger.LogInfo($"Applied {rules.Count} AI-generated rules in batch");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to apply rules batch", ex);
            throw;
        }
    }

    private static string InferSiteCategory(string domain)
    {
        if (domain.Contains("1337x") || domain.Contains("torrent") || domain.Contains("pirate") ||
            domain.Contains("rarbg") || domain.Contains("nyaa") || domain.Contains("yts"))
            return "Torrent/Download site - heavy popup ads, fake download buttons, overlay ads, interstitials, deceptive banners";

        if (domain.Contains("news") || domain.Contains("times") || domain.Contains("post") ||
            domain.Contains("cnn") || domain.Contains("bbc") || domain.Contains("reuters"))
            return "News site - article paywalls, newsletter popups, sidebar ads, consent banners";

        if (domain.Contains("reddit") || domain.Contains("twitter") || domain.Contains("facebook") ||
            domain.Contains("instagram") || domain.Contains("tiktok"))
            return "Social media - sponsored posts, tracking pixels, promoted content";

        if (domain.Contains("youtube") || domain.Contains("twitch") || domain.Contains("stream") ||
            domain.Contains("netflix") || domain.Contains("hulu"))
            return "Video/streaming - pre-roll ads, overlay banners, tracking, autoplay";

        if (domain.Contains("shop") || domain.Contains("amazon") || domain.Contains("ebay") ||
            domain.Contains("store") || domain.Contains("buy"))
            return "E-commerce - product tracking, retargeting pixels, newsletter popups, exit-intent overlays";

        return "General website - standard ad networks, analytics scripts, tracking pixels, cookie banners";
    }

    private List<Rule> ParseRulesFromResponse(string response)
    {
        try
        {
            var json = response.Trim();

            // Strip markdown code fences if present
            if (json.StartsWith("```"))
            {
                var startIdx = json.IndexOf('{') < json.IndexOf('[') || json.IndexOf('[') < 0
                    ? json.IndexOf('{') : json.IndexOf('[');
                var endIdx = Math.Max(json.LastIndexOf(']'), json.LastIndexOf('}'));
                if (startIdx >= 0 && endIdx > startIdx)
                {
                    json = json.Substring(startIdx, endIdx - startIdx + 1);
                }
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Try parsing as a JSON array first: [{ ... }, { ... }]
            var arrayStart = json.IndexOf('[');
            var arrayEnd = json.LastIndexOf(']');
            if (arrayStart >= 0 && arrayEnd > arrayStart)
            {
                var arrayJson = json.Substring(arrayStart, arrayEnd - arrayStart + 1);
                try
                {
                    var rules = JsonSerializer.Deserialize<List<Rule>>(arrayJson, options);
                    if (rules != null && rules.Count > 0)
                        return FinalizeRules(rules);
                }
                catch { /* Try next format */ }
            }

            // Try parsing as wrapper object: {"rules": [{ ... }]}
            if (json.StartsWith("{"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // Look for an array property (could be "rules", "Rules", "data", etc.)
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            var rulesJson = prop.Value.GetRawText();
                            var rules = JsonSerializer.Deserialize<List<Rule>>(rulesJson, options);
                            if (rules != null && rules.Count > 0)
                                return FinalizeRules(rules);
                        }
                    }
                }
                catch { /* Fall through */ }
            }

            ErrorLogger.LogError("Failed to parse AI rule response", new Exception($"No valid rules found in: {json.Substring(0, Math.Min(200, json.Length))}"));
            return new List<Rule>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to parse AI rule response", ex);
            return new List<Rule>();
        }
    }

    private List<Rule> FinalizeRules(List<Rule> rules)
    {
        var toRemove = new List<Rule>();

        foreach (var rule in rules)
        {
            if (string.IsNullOrEmpty(rule.Id))
                rule.Id = Guid.NewGuid().ToString();
            rule.Source = "ai";

            var countBefore = rule.Rules.Count;
            SanitizeRuleActions(rule);

            // Remove rules that had actions but lost them all to sanitization
            if (countBefore > 0 && rule.Rules.Count == 0)
                toRemove.Add(rule);
        }

        foreach (var r in toRemove)
            rules.Remove(r);

        return rules;
    }

    /// <summary>
    /// Sanitizes AI-generated rule actions to prevent destructive CSS/JS.
    /// Removes actions that would hide the entire page or break site functionality.
    /// </summary>
    private static void SanitizeRuleActions(Rule rule)
    {
        // Dangerous CSS patterns that hide/break entire pages
        var dangerousCssPatterns = new[]
        {
            "body { display: none", "body{display:none",
            "html { display: none", "html{display:none",
            "body { visibility: hidden", "body{visibility:hidden",
            "html { visibility: hidden", "html{visibility:hidden",
            "* { display: none", "*{display:none",
            "body { opacity: 0", "body{opacity:0",
            "body { overflow: hidden", "body{overflow:hidden",
        };

        // Dangerous JS patterns
        var dangerousJsPatterns = new[]
        {
            "document.body.style.display",
            "document.body.innerHTML",
            "document.documentElement.innerHTML",
            "document.write(",
            "window.location",
            "document.body.remove(",
        };

        var actionsToRemove = new List<RuleAction>();

        foreach (var action in rule.Rules)
        {
            if (action.Type == "inject_css" && !string.IsNullOrEmpty(action.Css))
            {
                var cssLower = action.Css.Replace(" ", "").ToLowerInvariant();
                if (dangerousCssPatterns.Any(p => cssLower.Contains(p.Replace(" ", "").ToLowerInvariant())))
                {
                    ErrorLogger.LogInfo($"Sanitized dangerous CSS from AI rule '{rule.Name}': {action.Css.Substring(0, Math.Min(100, action.Css.Length))}");
                    actionsToRemove.Add(action);
                }
            }

            if (action.Type == "inject_js" && !string.IsNullOrEmpty(action.Js))
            {
                if (dangerousJsPatterns.Any(p => action.Js.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorLogger.LogInfo($"Sanitized dangerous JS from AI rule '{rule.Name}': {action.Js.Substring(0, Math.Min(100, action.Js.Length))}");
                    actionsToRemove.Add(action);
                }
            }
        }

        foreach (var action in actionsToRemove)
        {
            rule.Rules.Remove(action);
        }
    }
}
