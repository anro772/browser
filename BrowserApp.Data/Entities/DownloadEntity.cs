namespace BrowserApp.Data.Entities;

/// <summary>
/// Entity representing a file download.
/// </summary>
public class DownloadEntity
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public string Status { get; set; } = "downloading";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
