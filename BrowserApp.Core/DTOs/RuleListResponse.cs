namespace BrowserApp.Core.DTOs;

/// <summary>
/// Response DTO for a paginated list of marketplace rules.
/// </summary>
public class RuleListResponse
{
    public List<RuleResponse> Rules { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
