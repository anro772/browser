namespace BrowserApp.Core.DTOs;

/// <summary>
/// Response DTO for a single marketplace rule.
/// </summary>
public class RuleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Site { get; set; } = "*";
    public int Priority { get; set; }
    public string RulesJson { get; set; } = "[]";
    public string AuthorUsername { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
