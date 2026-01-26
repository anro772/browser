namespace BrowserApp.Core.Models;

/// <summary>
/// Privacy protection levels that determine the aggressiveness of blocking rules.
/// </summary>
public enum PrivacyMode
{
    /// <summary>
    /// Minimal blocking - only essential trackers and ads.
    /// Best for sites that break with aggressive blocking.
    /// </summary>
    Relaxed,

    /// <summary>
    /// Balanced blocking - blocks most trackers and ads while maintaining site functionality.
    /// Recommended for daily browsing.
    /// </summary>
    Standard,

    /// <summary>
    /// Maximum blocking - aggressive tracker and ad blocking.
    /// May break some site functionality.
    /// </summary>
    Strict
}
