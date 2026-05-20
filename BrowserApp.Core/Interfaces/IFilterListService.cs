using BrowserApp.Core.Models;

namespace BrowserApp.Core.Interfaces;

public interface IFilterListService
{
    /// <summary>
    /// Whether the included EasyList/EasyPrivacy network blocking is currently active.
    /// When false, <see cref="ShouldBlock"/> always returns false even if patterns match.
    /// Toggled by the user via the "Ad Blocker" switch in Settings, which flows through
    /// <c>ExtensionService.SetAdBlockerEnabledAsync</c>.
    /// </summary>
    bool IsEnabled { get; set; }

    Task InitializeAsync();
    Task UpdateAllAsync();
    bool ShouldBlock(string requestUrl, string? pageUrl, string resourceType);
    string? GetCosmeticCss(string pageUrl);
    IReadOnlyList<FilterList> GetLists();
    Task SetListEnabledAsync(string listId, bool enabled);
    int GetTotalFilterCount();
}
