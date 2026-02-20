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

    private const string SystemPrompt = @"You are an expert ad-blocking and privacy rule generator for a browser. Generate comprehensive blocking rules similar to uBlock Origin filter lists.

Given a URL and page info, create rules covering ALL of these categories:

1. **Network Blocking Rules** (type: ""block"") - Block requests to ad/tracker domains:
   - Google ads: *doubleclick.net/*, *googlesyndication.com/*, *google-analytics.com/*, *googletagmanager.com/*, *googleadservices.com/*, *pagead2.googlesyndication.com/*
   - Facebook: *facebook.net/tr*, *facebook.com/tr*, *connect.facebook.net/*
   - Ad networks: *adnxs.com/*, *amazon-adsystem.com/*, *outbrain.com/*, *taboola.com/*, *criteo.com/*, *rubiconproject.com/*, *pubmatic.com/*, *openx.net/*
   - Analytics: *hotjar.com/*, *fullstory.com/*, *mixpanel.com/*, *segment.io/*
   - Also block site-specific ad patterns based on the URL

2. **CSS Cosmetic Filtering Rules** (type: ""inject_css"") - Hide ad containers and overlays with CSS:
   - Common ad selectors: [class*=""ad-""], [class*=""ad_""], [id*=""ad-""], [id*=""ad_""], .advertisement, .ad-container, .ad-wrapper, .sponsored
   - Popup overlays: .modal-overlay, .popup-overlay, .interstitial, [class*=""popup""], [class*=""overlay""]
   - Cookie/consent banners, newsletter signup nag screens
   - Fake download buttons, deceptive CTAs
   - Site-specific ad containers

3. **JavaScript Rules** (type: ""inject_js"") - Remove dynamic overlays and popups:
   - Anti-adblock detection removal
   - Scroll lock removal: document.body.style.overflow='auto'
   - Popup/overlay auto-dismiss scripts
   - Timer-based popup removal with MutationObserver

Each rule must follow this exact JSON schema:
{
  ""Name"": ""string - descriptive rule name"",
  ""Description"": ""string - what this rule does"",
  ""Site"": ""string - URL pattern with wildcards, e.g. *.example.com"",
  ""Priority"": number (10=normal, 50=important, 90=critical),
  ""Rules"": [
    {
      ""Type"": ""block"" | ""inject_css"" | ""inject_js"",
      ""Match"": {
        ""UrlPattern"": ""string - URL pattern to match (required for block, optional for inject)"",
        ""ResourceType"": ""Script"" | ""Stylesheet"" | ""Image"" | ""XHR"" | ""Fetch"" | null,
        ""Method"": ""GET"" | ""POST"" | null
      },
      ""Css"": ""CSS rules string (for inject_css only)"",
      ""Js"": ""JavaScript code string (for inject_js only)"",
      ""Timing"": ""dom_ready"" | ""load""
    }
  ]
}

IMPORTANT RULES:
- Generate 5-8 comprehensive rules
- Group related block patterns into single rules with MULTIPLE actions (e.g. one rule with 5-10 block actions for ad networks)
- CSS rules should use !important to override inline styles
- JS injection should be wrapped to avoid errors
- Be SPECIFIC to the site domain and its known ad patterns
- For torrent/streaming/download sites: focus on popup ads, overlay ads, fake download buttons, and interstitials
- For news/media sites: focus on paywall overlays, newsletter popups, and sidebar ads
- For social media: focus on sponsored content markers and tracking pixels

Respond with a JSON array ONLY. No markdown fences, no explanations, just the raw JSON array.";

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
        try
        {
            var domain = string.Empty;
            try { domain = new Uri(url).Host.ToLowerInvariant(); } catch { }

            var userMessage = $"Generate comprehensive blocking rules for: {url}";
            if (!string.IsNullOrEmpty(title))
                userMessage += $"\nPage title: {title}";
            if (!string.IsNullOrEmpty(domain))
            {
                userMessage += $"\nDomain: {domain}";
                userMessage += $"\nSite category: {InferSiteCategory(domain)}";
            }
            userMessage += "\nGenerate rules that block ALL ads, trackers, popups, and overlays on this site.";

            var messages = new List<OllamaChatMessage>
            {
                new() { Role = "system", Content = SystemPrompt },
                new() { Role = "user", Content = userMessage }
            };

            var response = await _ollamaClient.ChatAsync(messages);

            return ParseRulesFromResponse(response);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to generate rule suggestions", ex);
            return new List<Rule>();
        }
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
            // Try to extract JSON array from response
            var json = response.Trim();

            // Strip markdown code fences if present
            if (json.StartsWith("```"))
            {
                var startIdx = json.IndexOf('[');
                var endIdx = json.LastIndexOf(']');
                if (startIdx >= 0 && endIdx > startIdx)
                {
                    json = json.Substring(startIdx, endIdx - startIdx + 1);
                }
            }

            // Find the JSON array bounds
            var arrayStart = json.IndexOf('[');
            var arrayEnd = json.LastIndexOf(']');
            if (arrayStart >= 0 && arrayEnd > arrayStart)
            {
                json = json.Substring(arrayStart, arrayEnd - arrayStart + 1);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var rules = JsonSerializer.Deserialize<List<Rule>>(json, options);
            if (rules == null) return new List<Rule>();

            // Ensure each rule has an ID
            foreach (var rule in rules)
            {
                if (string.IsNullOrEmpty(rule.Id))
                    rule.Id = Guid.NewGuid().ToString();
                rule.Source = "ai";
            }

            return rules;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to parse AI rule response", ex);
            return new List<Rule>();
        }
    }
}
