using BrowserApp.Core.Models;

namespace BrowserApp.Core.Interfaces;

public interface IRuleGenerationService
{
    Task<List<Rule>> GenerateRuleSuggestionsAsync(string url, string? title = null);
    Task ApplyRuleAsync(Rule rule);
}
