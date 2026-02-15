using System.Text.Json.Serialization;

namespace BrowserApp.Core.DTOs.Ollama;

/// <summary>
/// Native Ollama /api/chat streaming chunk (NDJSON format).
/// Each line is a complete JSON object.
/// </summary>
public class OllamaStreamChunk
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("message")]
    public OllamaChatMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}
