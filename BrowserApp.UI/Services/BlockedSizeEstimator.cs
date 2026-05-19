namespace BrowserApp.UI.Services;

/// <summary>
/// Estimates the byte size of a blocked network request based on its resource type and URL.
/// Used by both <see cref="BlockingService"/> (in-memory session counter shown on the dashboard)
/// and <see cref="RequestInterceptor"/> (size written to the NetworkLog row) so the two figures stay
/// in lockstep instead of one defaulting to 5KB while the other uses per-type estimates.
/// </summary>
public static class BlockedSizeEstimator
{
    public static long Estimate(string resourceType, string url)
    {
        var lowerUrl = (url ?? string.Empty).ToLowerInvariant();

        return resourceType switch
        {
            "Script" => lowerUrl.Contains("analytics") || lowerUrl.Contains("tracking") ? 80_000 : 100_000,
            "Image" => lowerUrl.Contains("pixel") || lowerUrl.Contains("beacon") ? 1_000 : 50_000,
            "Media" => 500_000,
            "Stylesheet" => 20_000,
            "XHR" or "Fetch" => 5_000,
            "Font" => 30_000,
            "Document" => 50_000,
            "WebSocket" => 10_000,
            _ => 25_000
        };
    }
}
