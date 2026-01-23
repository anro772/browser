namespace BrowserApp.Server.DTOs.Responses;

/// <summary>
/// Paginated response containing a list of channels.
/// </summary>
public class ChannelListResponse
{
    /// <summary>
    /// List of channels.
    /// </summary>
    public List<ChannelResponse> Channels { get; set; } = new();

    /// <summary>
    /// Total number of channels matching the query.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
