using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using BrowserApp.Core.DTOs.Ollama;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.UI.Services;

public class OllamaClient : IOllamaClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;
    private readonly bool _ownsHttpClient;

    public OllamaClient(IConfiguration configuration)
    {
        var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _defaultModel = configuration["Ollama:Model"] ?? "llama3.2";
        _ownsHttpClient = true;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    public OllamaClient(HttpClient httpClient, string defaultModel = "llama3.2")
    {
        _httpClient = httpClient;
        _defaultModel = defaultModel;
        _ownsHttpClient = false;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(json);

            return modelsResponse?.Models.Select(m => m.Name).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Failed to get Ollama models", ex);
            return new List<string>();
        }
    }

    public async Task<string> ChatAsync(List<OllamaChatMessage> messages, string? model = null)
    {
        var request = new OllamaChatRequest
        {
            Model = model ?? _defaultModel,
            Messages = messages,
            Stream = false
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var chatResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson);

        return chatResponse?.Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        List<OllamaChatMessage> messages,
        string? model = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest
        {
            Model = model ?? _defaultModel,
            Messages = messages,
            Stream = true
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!line.StartsWith("data: "))
                continue;

            var data = line.Substring(6).Trim();

            if (data == "[DONE]")
                yield break;

            OllamaStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(data);
            }
            catch
            {
                continue;
            }

            var delta = chunk?.Choices.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta;
            }
        }
    }
}
