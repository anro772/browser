namespace BrowserApp.Core.Models;

public class FilterList
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public DateTime LastUpdated { get; set; }
    public int FilterCount { get; set; }
}
