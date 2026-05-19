namespace BrowserApp.UI.Models;

public class AutocompleteSuggestion
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "history", "bookmark", or "search"
    public int VisitCount { get; set; }

    /// <summary>Secondary line shown in the popup. For history/bookmarks this is the URL;
    /// for Google suggestions it's a "Search Google" hint instead of the long search URL.</summary>
    public string DisplayDescription => Source == "search" ? "Search Google" : Url;
}
