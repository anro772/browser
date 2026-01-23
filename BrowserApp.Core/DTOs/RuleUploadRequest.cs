namespace BrowserApp.Core.DTOs;

/// <summary>
/// Request DTO for uploading a rule to the marketplace.
/// </summary>
public class RuleUploadRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Site { get; set; } = "*";
    public int Priority { get; set; } = 10;
    public string RulesJson { get; set; } = "[]";
    public string AuthorUsername { get; set; } = string.Empty;
    public string[]? Tags { get; set; }
}
