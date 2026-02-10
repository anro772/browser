namespace BrowserApp.Core.Models;

/// <summary>
/// Represents a browser profile with isolated data.
/// Each profile has its own database, settings, and WebView2 user data.
/// </summary>
public class BrowserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Default";
    public string Color { get; set; } = "#0078D4";
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
