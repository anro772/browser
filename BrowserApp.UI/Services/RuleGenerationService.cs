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

    private const string SystemPrompt = @"You are a browser privacy rule generator. Given a URL, suggest blocking rules in JSON format.

Each rule must follow this exact JSON schema:
{
  ""Name"": ""string - descriptive rule name"",
  ""Description"": ""string - what the rule blocks"",
  ""Site"": ""string - URL pattern with wildcards, e.g. *.example.com"",
  ""Priority"": 10,
  ""Rules"": [
    {
      ""Type"": ""block"" | ""inject_css"" | ""inject_js"",
      ""Match"": {
        ""UrlPattern"": ""string - URL pattern to match, e.g. *tracker.com/*"",
        ""ResourceType"": ""script"" | ""stylesheet"" | ""image"" | ""xhr"" | ""fetch"" | null,
        ""Method"": ""GET"" | ""POST"" | null
      },
      ""Css"": ""string or null - CSS to inject (for inject_css type)"",
      ""Js"": ""string or null - JS to inject (for inject_js type)""
    }
  ]
}

Respond with a JSON array of 1-3 rules only. No markdown, no explanations, just the JSON array.
Focus on blocking trackers, analytics, ads, and unwanted third-party scripts for the given site.";

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
            var userMessage = $"Generate blocking rules for this website: {url}";
            if (!string.IsNullOrEmpty(title))
            {
                userMessage += $" (Title: {title})";
            }

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

            using var scope = _scopeFactory.CreateScope();
            var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
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
