using System.Diagnostics;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for coordinating request blocking decisions.
/// Thin wrapper around RuleEngine with session statistics.
/// </summary>
public class BlockingService : IBlockingService
{
    private readonly IRuleEngine _ruleEngine;
    private int _blockedCount;
    private long _bytesSaved;
    private readonly object _statsLock = new();

    public BlockingService(IRuleEngine ruleEngine)
    {
        _ruleEngine = ruleEngine;
    }

    public async Task InitializeAsync()
    {
        await _ruleEngine.InitializeAsync();
        var ruleCount = _ruleEngine.GetActiveRules().Count();
        ErrorLogger.LogInfo($"BlockingService initialized with {ruleCount} active rules");
    }

    public RuleEvaluationResult ShouldBlockRequest(NetworkRequest request, string? currentPageUrl)
    {
        try
        {
            var result = _ruleEngine.Evaluate(request, currentPageUrl);

            if (result.ShouldBlock)
            {
                lock (_statsLock)
                {
                    _blockedCount++;
                    // Estimate bytes saved (use content-length or default estimate)
                    _bytesSaved += request.Size ?? 5000; // Default 5KB estimate
                }

                ErrorLogger.LogInfo($"BLOCKED: {request.Url} by rule: {result.BlockedByRuleName}");
                Debug.WriteLine($"[BlockingService] Blocked: {request.Url} by rule: {result.BlockedByRuleName}");
            }

            return result;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("BlockingService evaluation error", ex);
            Debug.WriteLine($"[BlockingService] Error evaluating request: {ex.Message}");
            // Fail-open: allow the request if evaluation fails
            return RuleEvaluationResult.Allow();
        }
    }

    public int GetBlockedCount()
    {
        lock (_statsLock)
        {
            return _blockedCount;
        }
    }

    public long GetBytesSaved()
    {
        lock (_statsLock)
        {
            return _bytesSaved;
        }
    }

    public void ResetStats()
    {
        lock (_statsLock)
        {
            _blockedCount = 0;
            _bytesSaved = 0;
        }
    }
}
