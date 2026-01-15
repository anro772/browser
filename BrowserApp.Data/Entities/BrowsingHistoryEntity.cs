namespace BrowserApp.Data.Entities;

/// <summary>
/// Entity representing a browsing history entry.
/// </summary>
public class BrowsingHistoryEntity
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
}
