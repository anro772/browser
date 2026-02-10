namespace BrowserApp.UI.Models;

public class AutocompleteSuggestion
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "history" or "bookmark"
    public int VisitCount { get; set; }
}
