namespace BrowserApp.Core.Models;

/// <summary>
/// Result of evaluating a request against active rules.
/// </summary>
public class RuleEvaluationResult
{
    /// <summary>
    /// Whether the request should be blocked.
    /// </summary>
    public bool ShouldBlock { get; set; }

    /// <summary>
    /// ID of the rule that blocked this request (if blocked).
    /// </summary>
    public string? BlockedByRuleId { get; set; }

    /// <summary>
    /// Name of the rule that blocked this request (if blocked).
    /// </summary>
    public string? BlockedByRuleName { get; set; }

    /// <summary>
    /// CSS/JS injections to apply for this page.
    /// </summary>
    public List<RuleAction> InjectionsToApply { get; set; } = new();

    /// <summary>
    /// Creates a result indicating no blocking.
    /// </summary>
    public static RuleEvaluationResult Allow() => new() { ShouldBlock = false };

    /// <summary>
    /// Creates a result indicating the request should be blocked.
    /// </summary>
    public static RuleEvaluationResult Block(string ruleId, string ruleName) => new()
    {
        ShouldBlock = true,
        BlockedByRuleId = ruleId,
        BlockedByRuleName = ruleName
    };
}
