namespace BrowserApp.UI.DTOs;

/// <summary>
/// Response DTO for a paginated list of marketplace rules.
/// </summary>
public class RuleListResponse
{
    /// <summary>
    /// List of rules.
    /// </summary>
    public List<RuleResponse> Rules { get; set; } = new();

    /// <summary>
    /// Total count of rules matching the query.
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
