namespace BrowserApp.Data.Entities;

/// <summary>
/// Entity representing a saved tab session.
/// </summary>
public class TabSessionEntity
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int TabOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
