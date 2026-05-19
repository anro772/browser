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
    private int _detectedCount;
    private long _bytesSaved;
    private readonly object _statsLock = new();

    public event EventHandler<NetworkRequest>? RequestBlocked;

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

            lock (_statsLock)
            {
                _detectedCount++;
            }

            if (result.ShouldBlock)
            {
                lock (_statsLock)
                {
                    _blockedCount++;
                    // request.Size is null at this point (response not yet seen); fall back to the
                    // per-resource-type estimate so the dashboard byte total matches what gets written
                    // into the NetworkLog row by RequestInterceptor. Without this they diverged ~5x.
                    _bytesSaved += request.Size ?? BlockedSizeEstimator.Estimate(request.ResourceType, request.Url);
                }

                ErrorLogger.LogInfo($"BLOCKED: {request.Url} by rule: {result.BlockedByRuleName}");
                Debug.WriteLine($"[BlockingService] Blocked: {request.Url} by rule: {result.BlockedByRuleName}");

                try
                {
                    RequestBlocked?.Invoke(this, request);
                }
                catch (Exception evtEx)
                {
                    Debug.WriteLine($"[BlockingService] RequestBlocked handler error: {evtEx.Message}");
                }
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

    public int GetDetectedCount()
    {
        lock (_statsLock)
        {
            return _detectedCount;
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
            _detectedCount = 0;
            _bytesSaved = 0;
        }
    }
}
