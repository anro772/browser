namespace BrowserApp.Core.Models;

/// <summary>
/// Represents a single HTTP header modification for privacy enforcement.
/// </summary>
public class HeaderModification
{
    /// <summary>
    /// Operation to perform: "set", "remove", or "append".
    /// </summary>
    public string Operation { get; set; } = "set";

    /// <summary>
    /// Header name (e.g., "DNT", "Sec-GPC", "Referer").
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Header value (required for "set" and "append", ignored for "remove").
    /// </summary>
    public string? Value { get; set; }
}
