using BrowserApp.UI.Models;

namespace BrowserApp.UI.Services;

public record PageContext(
    string Url,
    string Title,
    string? Selection,
    string Text,
    string? ScreenshotBase64);

public interface IPageContextService
{
    Task<PageContext?> CaptureAsync(BrowserTabItem? tab, bool includeScreenshot, CancellationToken cancellationToken = default);
}
