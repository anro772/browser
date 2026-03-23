namespace BrowserApp.Core.DTOs;

public class AddChannelRuleRequest
{
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Site { get; set; } = "*";
    public int Priority { get; set; } = 10;
    public string RulesJson { get; set; } = "[]";
    public bool IsEnforced { get; set; } = true;
}
