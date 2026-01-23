using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.Core.DTOs;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for synchronizing rules between local storage and the marketplace.
/// </summary>
public class RuleSyncService : IRuleSyncService
{
    private readonly IMarketplaceApiClient _apiClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuleEngine _ruleEngine;

    public RuleSyncService(
        IMarketplaceApiClient apiClient,
        IServiceScopeFactory scopeFactory,
        IRuleEngine ruleEngine)
    {
        _apiClient = apiClient;
        _scopeFactory = scopeFactory;
        _ruleEngine = ruleEngine;
    }

    public async Task<Rule?> DownloadAndInstallRuleAsync(Guid marketplaceRuleId)
    {
        try
        {
            // Get the rule from the marketplace
            var response = await _apiClient.GetRuleByIdAsync(marketplaceRuleId);
            if (response == null)
            {
                ErrorLogger.LogInfo($"Rule {marketplaceRuleId} not found in marketplace");
                return null;
            }

            // Convert to local Rule model
            var rule = ConvertToRule(response);

            // Save to local database
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            // Check if already installed
            var existing = await repository.GetByIdAsync(rule.Id);
            if (existing != null)
            {
                ErrorLogger.LogInfo($"Rule {rule.Name} is already installed");
                return MapEntityToRule(existing);
            }

            var entity = ConvertToEntity(rule, marketplaceRuleId);
            await repository.AddAsync(entity);

            // Increment download count on server
            await _apiClient.IncrementDownloadAsync(marketplaceRuleId);

            // Reload rules in engine
            await _ruleEngine.ReloadRulesAsync();

            ErrorLogger.LogInfo($"Rule {rule.Name} installed from marketplace");
            return rule;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to download and install rule {marketplaceRuleId}", ex);
            return null;
        }
    }

    public async Task<IEnumerable<Rule>> GetInstalledMarketplaceRulesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

        var entities = await repository.GetBySourceAsync("marketplace");
        return entities.Select(MapEntityToRule);
    }

    public async Task<bool> UninstallMarketplaceRuleAsync(string ruleId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            var existing = await repository.GetByIdAsync(ruleId);
            if (existing == null || existing.Source != "marketplace")
            {
                return false;
            }

            await repository.DeleteAsync(ruleId);

            // Reload rules in engine
            await _ruleEngine.ReloadRulesAsync();

            ErrorLogger.LogInfo($"Rule {existing.Name} uninstalled");
            return true;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to uninstall rule {ruleId}", ex);
            return false;
        }
    }

    public async Task<bool> UploadRuleAsync(Rule rule, string username)
    {
        try
        {
            var request = new RuleUploadRequest
            {
                Name = rule.Name,
                Description = rule.Description,
                Site = rule.Site,
                Priority = rule.Priority,
                RulesJson = JsonSerializer.Serialize(rule.Rules),
                AuthorUsername = username,
                Tags = Array.Empty<string>()
            };

            var response = await _apiClient.UploadRuleAsync(request);
            return response != null;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to upload rule {rule.Name}", ex);
            return false;
        }
    }

    public async Task<bool> IsServerAvailableAsync()
    {
        return await _apiClient.CheckConnectionAsync();
    }

    private Rule ConvertToRule(RuleResponse response)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var ruleActions = JsonSerializer.Deserialize<List<RuleAction>>(response.RulesJson, options)
            ?? new List<RuleAction>();

        return new Rule
        {
            Id = response.Id.ToString(),
            Name = response.Name,
            Description = response.Description,
            Site = response.Site,
            Priority = response.Priority,
            Rules = ruleActions,
            Source = "marketplace",
            Enabled = true,
            IsEnforced = false,
            CreatedAt = response.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private RuleEntity ConvertToEntity(Rule rule, Guid marketplaceId)
    {
        return new RuleEntity
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            Site = rule.Site,
            Priority = rule.Priority,
            RulesJson = JsonSerializer.Serialize(rule.Rules),
            Source = "marketplace",
            MarketplaceId = marketplaceId.ToString(),
            Enabled = true,
            IsEnforced = false,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Rule MapEntityToRule(RuleEntity entity)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var ruleActions = new List<RuleAction>();

        if (!string.IsNullOrEmpty(entity.RulesJson))
        {
            ruleActions = JsonSerializer.Deserialize<List<RuleAction>>(entity.RulesJson, options)
                ?? new List<RuleAction>();
        }

        return new Rule
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Site = entity.Site,
            Priority = entity.Priority,
            Rules = ruleActions,
            Source = entity.Source,
            Enabled = entity.Enabled,
            IsEnforced = entity.IsEnforced,
            ChannelId = entity.ChannelId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
