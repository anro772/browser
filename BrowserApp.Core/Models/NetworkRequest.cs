namespace BrowserApp.Core.Models;

/// <summary>
/// Represents a captured network request.
/// Used across UI, services, and data layers.
/// </summary>
public class NetworkRequest
{
    public string Url { get; init; } = string.Empty;
    public string Method { get; init; } = "GET";
    public int? StatusCode { get; init; }
    public string ResourceType { get; init; } = "Unknown";
    public string? ContentType { get; init; }
    public long? Size { get; init; }
    public bool WasBlocked { get; init; }
    public string? BlockedByRuleId { get; init; }

    /// <summary>
    /// Human-readable label for what blocked this request — the custom rule name, the filter list
    /// name ("EasyList/EasyPrivacy"), or a content-policy category. Used by the expanded monitor's
    /// "Blocked by" column and detail pane.
    /// </summary>
    public string? BlockedByRule { get; init; }

    /// <summary>
    /// The actual pattern/host that matched when blocking. Optional — only populated for filter
    /// list and rule blocks where we know the matching pattern.
    /// </summary>
    public string? BlockedByPattern { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Formatted local timestamp used by the expanded monitor's detail pane.
    /// </summary>
    public string TimestampDisplay => Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// Gets the host/domain from the URL.
    /// </summary>
    public string Host
    {
        get
        {
            try
            {
                return new Uri(Url).Host;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Determines if this request is to a third-party domain.
    /// </summary>
    /// <param name="pageHost">The host of the current page.</param>
    public bool IsThirdParty(string pageHost)
    {
        if (string.IsNullOrEmpty(pageHost)) return false;

        try
        {
            var requestHost = new Uri(Url).Host;
            return !requestHost.Equals(pageHost, StringComparison.OrdinalIgnoreCase) &&
                   !requestHost.EndsWith("." + pageHost, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the size formatted as a human-readable string.
    /// </summary>
    public string FormattedSize
    {
        get
        {
            if (!Size.HasValue) return "-";

            return Size.Value switch
            {
                < 1024 => $"{Size.Value} B",
                < 1024 * 1024 => $"{Size.Value / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{Size.Value / (1024.0 * 1024):F1} MB",
                _ => $"{Size.Value / (1024.0 * 1024 * 1024):F2} GB"
            };
        }
    }

    /// <summary>
    /// Gets a shortened URL for display (path only).
    /// </summary>
    public string ShortUrl
    {
        get
        {
            try
            {
                var uri = new Uri(Url);
                var path = uri.PathAndQuery;
                return path.Length > 60 ? path[..57] + "..." : path;
            }
            catch
            {
                return Url.Length > 60 ? Url[..57] + "..." : Url;
            }
        }
    }
}
