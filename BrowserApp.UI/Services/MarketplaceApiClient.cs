using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using BrowserApp.Core.DTOs;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.UI.Services;

/// <summary>
/// HTTP client for communicating with the marketplace API server.
/// </summary>
public class MarketplaceApiClient : IMarketplaceApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string? _serverUrl;
    private bool _disposed;

    public MarketplaceApiClient(IConfiguration configuration)
    {
        _serverUrl = configuration["MarketplaceApi:BaseUrl"] ?? "http://localhost:5000";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_serverUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public string? ServerUrl => _serverUrl;

    public async Task<RuleListResponse?> GetRulesAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/marketplace/rules?page={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RuleListResponse>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to get marketplace rules", ex);
            return new RuleListResponse { Rules = new(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
    }

    public async Task<RuleResponse?> GetRuleByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/marketplace/rules/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RuleResponse>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to get rule {id}", ex);
            return null;
        }
    }

    public async Task<RuleResponse?> UploadRuleAsync(RuleUploadRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/marketplace/rules", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RuleResponse>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to upload rule {request.Name}", ex);
            return null;
        }
    }

    public async Task<RuleListResponse?> SearchRulesAsync(string? query, string[]? tags, int page = 1, int pageSize = 20)
    {
        try
        {
            var url = $"/api/marketplace/search?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(query))
            {
                url += $"&q={Uri.EscapeDataString(query)}";
            }
            if (tags != null && tags.Length > 0)
            {
                url += $"&tags={Uri.EscapeDataString(string.Join(",", tags))}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RuleListResponse>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to search marketplace rules", ex);
            return new RuleListResponse { Rules = new(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
    }

    public async Task<RuleResponse?> IncrementDownloadAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/marketplace/rules/{id}/download", null);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RuleResponse>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to increment download for rule {id}", ex);
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
