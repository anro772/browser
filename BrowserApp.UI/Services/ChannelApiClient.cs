using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using BrowserApp.Core.DTOs;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.UI.Services;

/// <summary>
/// HTTP client for channel API operations.
/// </summary>
public class ChannelApiClient : IChannelApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string? _serverUrl;
    private bool _disposed;

    public ChannelApiClient(IConfiguration configuration)
    {
        _serverUrl = configuration["MarketplaceApi:BaseUrl"] ?? "http://localhost:5000";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_serverUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public string? ServerUrl => _serverUrl;

    public async Task<ChannelListResponse?> GetChannelsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/channel/channels?page={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChannelListResponse>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to get channels", ex);
            return new ChannelListResponse { Channels = new(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
    }

    public async Task<ChannelResponse?> GetChannelByIdAsync(Guid channelId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/channel/channels/{channelId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChannelResponse>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to get channel {channelId}", ex);
            return null;
        }
    }

    public async Task<ChannelResponse?> CreateChannelAsync(CreateChannelRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/channel/channels", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChannelResponse>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to create channel '{request.Name}'", ex);
            return null;
        }
    }

    public async Task<bool> JoinChannelAsync(Guid channelId, string username, string password)
    {
        try
        {
            var request = new JoinChannelRequest
            {
                Username = username,
                Password = password
            };
            var response = await _httpClient.PostAsJsonAsync($"/api/channel/channels/{channelId}/join", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to join channel {channelId}", ex);
            return false;
        }
    }

    public async Task<bool> LeaveChannelAsync(Guid channelId, string username)
    {
        try
        {
            var request = new LeaveChannelRequest { Username = username };
            var response = await _httpClient.PostAsJsonAsync($"/api/channel/channels/{channelId}/leave", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to leave channel {channelId}", ex);
            return false;
        }
    }

    public async Task<ChannelListResponse?> GetUserChannelsAsync(string username)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/channel/user/{username}/channels");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChannelListResponse>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to get channels for user '{username}'", ex);
            return new ChannelListResponse { Channels = new(), TotalCount = 0, Page = 1, PageSize = 100 };
        }
    }

    public async Task<ChannelRuleListResponse?> GetChannelRulesAsync(Guid channelId, string username)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/channel/channels/{channelId}/rules?username={Uri.EscapeDataString(username)}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChannelRuleListResponse>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to get rules for channel {channelId}", ex);
            return null;
        }
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
