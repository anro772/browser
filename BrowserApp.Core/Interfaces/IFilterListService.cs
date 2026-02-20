using BrowserApp.Core.Models;

namespace BrowserApp.Core.Interfaces;

public interface IFilterListService
{
    Task InitializeAsync();
    Task UpdateAllAsync();
    bool ShouldBlock(string requestUrl, string? pageUrl, string resourceType);
    string? GetCosmeticCss(string pageUrl);
    IReadOnlyList<FilterList> GetLists();
    Task SetListEnabledAsync(string listId, bool enabled);
    int GetTotalFilterCount();
}
