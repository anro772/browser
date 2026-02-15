using System.Text.Json.Serialization;

namespace BrowserApp.Core.DTOs.Ollama;

public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "llama3.2";

    [JsonPropertyName("messages")]
    public List<OllamaChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
}

public class OllamaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
