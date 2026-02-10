namespace BrowserApp.Data.Entities;

/// <summary>
/// Entity representing an installed browser extension.
/// </summary>
public class ExtensionEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
}
