using BrowserApp.Core.DTOs.Ollama;

namespace BrowserApp.Core.Interfaces;

public interface IOllamaClient
{
    Task<bool> IsAvailableAsync();
    Task<List<string>> GetModelsAsync();
    Task<string> ChatAsync(List<OllamaChatMessage> messages, string? model = null);
    IAsyncEnumerable<string> ChatStreamAsync(List<OllamaChatMessage> messages, string? model = null, CancellationToken cancellationToken = default);
}
