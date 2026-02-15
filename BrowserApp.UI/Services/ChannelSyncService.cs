using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.DTOs;
using BrowserApp.Core.Interfaces;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for synchronizing channel rules between server and local storage.
/// Simplified for MVP - manual sync only (no background timer).
/// </summary>
public class ChannelSyncService : IChannelSyncService
{
    private readonly IChannelApiClient _apiClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuleEngine _ruleEngine;

    public ChannelSyncService(
        IChannelApiClient apiClient,
        IServiceScopeFactory scopeFactory,
        IRuleEngine ruleEngine)
    {
        _apiClient = apiClient;
        _scopeFactory = scopeFactory;
        _ruleEngine = ruleEngine;
    }

    public async Task<bool> JoinChannelAsync(Guid channelId, string channelName, string channelDescription, string username, string password)
    {
        try
        {
            // Join on server
            var success = await _apiClient.JoinChannelAsync(channelId, username, password);
            if (!success)
            {
                ErrorLogger.LogInfo($"Failed to join channel {channelId} - invalid password or channel not found");
                return false;
            }

            // Save membership locally
            using var scope = _scopeFactory.CreateScope();
            var membershipRepo = scope.ServiceProvider.GetRequiredService<IChannelMembershipRepository>();

            var existing = await membershipRepo.GetByChannelIdAsync(channelId.ToString());
            if (existing == null)
            {
                var membership = new ChannelMembershipEntity
                {
                    ChannelId = channelId.ToString(),
                    ChannelName = channelName,
                    ChannelDescription = channelDescription,
                    Username = username,
                    JoinedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                };
                await membershipRepo.AddAsync(membership);
            }

            // Initial sync
            await SyncChannelRulesAsync(channelId.ToString(), username);

            ErrorLogger.LogInfo($"Successfully joined channel '{channelName}'");
            return true;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to join channel {channelId}", ex);
            return false;
        }
    }

    public async Task<bool> LeaveChannelAsync(string channelId, string username)
    {
        try
        {
            // Leave on server — don't touch local data if server call fails
            var channelGuid = Guid.Parse(channelId);
            var success = await _apiClient.LeaveChannelAsync(channelGuid, username);
            if (!success) return false;

            // Delete local rules from this channel
            using var scope = _scopeFactory.CreateScope();
            var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
            var membershipRepo = scope.ServiceProvider.GetRequiredService<IChannelMembershipRepository>();

            await ruleRepo.DeleteByChannelIdAsync(channelId);
            await membershipRepo.DeleteByChannelIdAsync(channelId);

            // Reload rules
            await _ruleEngine.ReloadRulesAsync();

            ErrorLogger.LogInfo($"Successfully left channel {channelId}");
            return true;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to leave channel {channelId}", ex);
            return false;
        }
    }

    public async Task<bool> SyncChannelRulesAsync(string channelId, string username)
    {
        try
        {
            var channelGuid = Guid.Parse(channelId);
            var rulesResponse = await _apiClient.GetChannelRulesAsync(channelGuid, username);

            if (rulesResponse == null)
            {
                ErrorLogger.LogInfo($"Failed to get rules for channel {channelId}");
                return false;
            }

            using var scope = _scopeFactory.CreateScope();
            var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
            var membershipRepo = scope.ServiceProvider.GetRequiredService<IChannelMembershipRepository>();

            // Server wins: delete all local rules from this channel
            await ruleRepo.DeleteByChannelIdAsync(channelId);

            // Insert new rules
            foreach (var serverRule in rulesResponse.Rules)
            {
                var entity = new RuleEntity
                {
                    Id = serverRule.Id.ToString(),
                    Name = serverRule.Name,
                    Description = serverRule.Description,
                    Site = serverRule.Site,
                    Priority = serverRule.Priority,
                    RulesJson = serverRule.RulesJson,
                    Source = "channel",
                    ChannelId = channelId,
                    IsEnforced = serverRule.IsEnforced,
                    Enabled = true,
                    CreatedAt = serverRule.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };
                await ruleRepo.AddAsync(entity);
            }

            // Update sync time
            await membershipRepo.UpdateLastSyncedAsync(channelId, rulesResponse.Rules.Count);

            // Reload rules in engine
            await _ruleEngine.ReloadRulesAsync();

            ErrorLogger.LogInfo($"Synced {rulesResponse.Rules.Count} rules from channel '{rulesResponse.ChannelName}'");
            return true;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to sync rules for channel {channelId}", ex);
            return false;
        }
    }

    public async Task SyncAllChannelsAsync(string username)
    {
        try
        {
            if (!await IsServerAvailableAsync())
            {
                ErrorLogger.LogInfo("Channel sync skipped - server unavailable");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var membershipRepo = scope.ServiceProvider.GetRequiredService<IChannelMembershipRepository>();
            var memberships = await membershipRepo.GetActiveAsync();

            foreach (var membership in memberships)
            {
                await SyncChannelRulesAsync(membership.ChannelId, username);
            }

            ErrorLogger.LogInfo($"Synced {memberships.Count()} channels");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to sync all channels", ex);
        }
    }

    public async Task<IEnumerable<ChannelMembershipDto>> GetJoinedChannelsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var membershipRepo = scope.ServiceProvider.GetRequiredService<IChannelMembershipRepository>();
        var entities = await membershipRepo.GetActiveAsync();

        return entities.Select(e => new ChannelMembershipDto
        {
            Id = e.Id,
            ChannelId = e.ChannelId,
            ChannelName = e.ChannelName,
            ChannelDescription = e.ChannelDescription,
            Username = e.Username,
            IsActive = e.IsActive,
            JoinedAt = e.JoinedAt,
            LastSyncedAt = e.LastSyncedAt,
            RuleCount = e.RuleCount
        });
    }

    public async Task<bool> IsServerAvailableAsync()
    {
        return await _apiClient.CheckConnectionAsync();
    }

    public async Task SaveLocalMembershipAsync(string channelId, string channelName, string channelDescription, string username)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var membershipRepo = scope.ServiceProvider.GetRequiredService<IChannelMembershipRepository>();

            var existing = await membershipRepo.GetByChannelIdAsync(channelId);
            if (existing == null)
            {
                var membership = new ChannelMembershipEntity
                {
                    ChannelId = channelId,
                    ChannelName = channelName,
                    ChannelDescription = channelDescription,
                    Username = username,
                    JoinedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                };
                await membershipRepo.AddAsync(membership);
                ErrorLogger.LogInfo($"Saved local membership for channel '{channelName}'");
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to save local membership for channel {channelId}", ex);
        }
    }
}
