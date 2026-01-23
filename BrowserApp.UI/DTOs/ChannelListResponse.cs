namespace BrowserApp.UI.DTOs;

/// <summary>
/// Paginated response containing a list of channels.
/// </summary>
public class ChannelListResponse
{
    public List<ChannelResponse> Channels { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
