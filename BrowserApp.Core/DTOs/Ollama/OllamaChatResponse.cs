using System.Text.Json.Serialization;

namespace BrowserApp.Core.DTOs.Ollama;

/// <summary>
/// Native Ollama /api/chat response (non-streaming).
/// </summary>
public class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("message")]
    public OllamaChatMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("total_duration")]
    public long? TotalDuration { get; set; }

    [JsonPropertyName("eval_count")]
    public int? EvalCount { get; set; }
}
