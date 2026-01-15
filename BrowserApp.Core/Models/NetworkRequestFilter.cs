namespace BrowserApp.Core.Models;

/// <summary>
/// Filter options for network log queries.
/// </summary>
public enum NetworkRequestFilter
{
    All,
    Blocked,
    ThirdParty,
    Scripts,
    Images
}
