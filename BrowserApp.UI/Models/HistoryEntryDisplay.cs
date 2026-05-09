using BrowserApp.Data.Entities;

namespace BrowserApp.UI.Models;

/// <summary>
/// View model wrapper around <see cref="BrowsingHistoryEntity"/> with derived display
/// fields used by the history sidebar (host, title fallback, day grouping bucket).
/// </summary>
public class HistoryEntryDisplay
{
    public BrowsingHistoryEntity Entity { get; }

    public HistoryEntryDisplay(BrowsingHistoryEntity entity)
    {
        Entity = entity;
    }

    public string Url => Entity.Url;
    public DateTime VisitedAt => Entity.VisitedAt;

    /// <summary>
    /// Title to render. Falls back to the URL host when the page didn't supply a title
    /// (this used to render the raw URL twice — see screenshot in PR review).
    /// </summary>
    public string DisplayTitle =>
        !string.IsNullOrWhiteSpace(Entity.Title) && Entity.Title != Entity.Url
            ? Entity.Title!
            : Host;

    public string Host
    {
        get
        {
            if (Uri.TryCreate(Entity.Url, UriKind.Absolute, out var uri))
            {
                var host = uri.Host;
                return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                    ? host.Substring(4)
                    : host;
            }
            return Entity.Url;
        }
    }

    /// <summary>
    /// First letter of the host, used for the round avatar chip (poor-man's favicon).
    /// </summary>
    public string HostInitial =>
        !string.IsNullOrEmpty(Host) ? Host.Substring(0, 1).ToUpperInvariant() : "?";

    /// <summary>
    /// Day bucket label: "Today", "Yesterday", weekday name (within last week), or full date.
    /// Used as the GroupName by the CollectionView.
    /// </summary>
    public string GroupLabel
    {
        get
        {
            var today = DateTime.Now.Date;
            var visited = VisitedAt.ToLocalTime().Date;
            var delta = (today - visited).TotalDays;

            if (delta < 1) return "Today";
            if (delta < 2) return "Yesterday";
            if (delta < 7) return visited.ToString("dddd"); // e.g. "Tuesday"
            return visited.ToString("MMMM d, yyyy");
        }
    }

    /// <summary>
    /// Time-of-day rendered next to the entry.
    /// </summary>
    public string TimeLabel => VisitedAt.ToLocalTime().ToString("HH:mm");
}
