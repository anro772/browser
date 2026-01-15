namespace BrowserApp.Data.Entities;

/// <summary>
/// Entity representing a logged network request.
/// Stores all captured HTTP requests for monitoring and analysis.
/// </summary>
public class NetworkLogEntity
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public int? StatusCode { get; set; }
    public string ResourceType { get; set; } = "Unknown";
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public bool WasBlocked { get; set; }
    public string? BlockedByRuleId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
