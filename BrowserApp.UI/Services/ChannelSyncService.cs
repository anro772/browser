using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.DTOs;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for synchronizing channel rules between server and local storage.
/// </summary>
public class ChannelSyncService : IChannelSyncService
{
    private readonly ChannelApiClient _apiClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuleEngine _ruleEngine;
    private PeriodicTimer? _syncTimer;
    private Task? _syncTask;
    private CancellationTokenSource? _cts;
    private string _currentUsername = string.Empty;

    public ChannelSyncService(
        ChannelApiClient apiClient,
        IServiceScopeFactory scopeFactory,
        IRuleEngine ruleEngine)
    {
        _apiClient = apiClient;
        _scopeFactory = scopeFactory;
        _ruleEngine = ruleEngine;
    }

    public Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        _syncTimer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        _syncTask = RunSyncLoopAsync(_cts.Token);
        ErrorLogger.LogInfo("Channel sync service started (15-minute interval)");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _syncTimer?.Dispose();

            if (_syncTask != null)
            {
                try
                {
                    await _syncTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            }

            _cts.Dispose();
            _cts = null;
        }
        ErrorLogger.LogInfo("Channel sync service stopped");
    }

    private async Task RunSyncLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _syncTimer != null)
            {
                await _syncTimer.WaitForNextTickAsync(ct);
                if (!string.IsNullOrEmpty(_currentUsername))
                {
                    await SyncAllChannelsAsync(_currentUsername);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Channel sync loop error", ex);
        }
    }

    public void SetUsername(string username)
    {
        _currentUsername = username;
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

            _currentUsername = username;
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
            // Leave on server
            var channelGuid = Guid.Parse(channelId);
            await _apiClient.LeaveChannelAsync(channelGuid, username);

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
            var rulesResponse = await _apiClient.GetChannelRulesTypedAsync(channelGuid, username);

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
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
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

    public async Task<IEnumerable<object>> GetJoinedChannelsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var membershipRepo = scope.ServiceProvider.GetRequiredService<IChannelMembershipRepository>();
        return await membershipRepo.GetActiveAsync();
    }

    public async Task<IEnumerable<ChannelMembershipEntity>> GetJoinedChannelsTypedAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var membershipRepo = scope.ServiceProvider.GetRequiredService<IChannelMembershipRepository>();
        return await membershipRepo.GetActiveAsync();
    }

    public async Task<bool> IsServerAvailableAsync()
    {
        return await _apiClient.CheckConnectionAsync();
    }
}
