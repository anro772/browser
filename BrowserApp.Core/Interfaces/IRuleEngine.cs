using BrowserApp.Core.Models;

namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Rule evaluation engine interface.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Initializes the engine and loads rules from the repository.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Reloads rules from the repository (call when rules are changed).
    /// </summary>
    Task ReloadRulesAsync();

    /// <summary>
    /// Evaluates a network request against active rules.
    /// </summary>
    /// <param name="request">The network request to evaluate.</param>
    /// <param name="currentPageUrl">The URL of the current page.</param>
    /// <returns>The evaluation result with blocking decision and injections.</returns>
    RuleEvaluationResult Evaluate(NetworkRequest request, string? currentPageUrl);

    /// <summary>
    /// Gets all injections that should be applied for a given page URL.
    /// </summary>
    /// <param name="pageUrl">The page URL to get injections for.</param>
    /// <returns>List of CSS/JS injection actions to apply.</returns>
    IEnumerable<RuleAction> GetInjectionsForPage(string pageUrl);

    /// <summary>
    /// Gets all currently active (enabled) rules.
    /// </summary>
    IEnumerable<Rule> GetActiveRules();

    /// <summary>
    /// Gets the total count of loaded rules.
    /// </summary>
    int GetRuleCount();

    /// <summary>
    /// Event raised when rules are reloaded.
    /// </summary>
    event EventHandler? RulesReloaded;
}
