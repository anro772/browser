using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using BrowserApp.UI.Models;

namespace BrowserApp.UI.Services;

public class PageContextService : IPageContextService
{
    private const int MaxTextChars = 6000;

    public async Task<PageContext?> CaptureAsync(BrowserTabItem? tab, bool includeScreenshot, CancellationToken cancellationToken = default)
    {
        if (tab?.CoreWebView2 == null) return null;

        var url = tab.CoreWebView2.Source ?? string.Empty;
        if (!IsRealPage(url)) return null;

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var extracted = await RunOnUIThreadAsync(() => ExtractDomAsync(tab.CoreWebView2!));
            if (extracted == null) return null;

            string? screenshot = null;
            if (includeScreenshot)
            {
                screenshot = await RunOnUIThreadAsync(() => CaptureScreenshotAsync(tab.CoreWebView2!));
            }

            var text = Truncate(extracted.Text, MaxTextChars);

            return new PageContext(
                Url: string.IsNullOrEmpty(extracted.Url) ? url : extracted.Url,
                Title: extracted.Title,
                Selection: string.IsNullOrWhiteSpace(extracted.Selection) ? null : extracted.Selection,
                Text: text,
                ScreenshotBase64: screenshot);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("PageContextService capture failed", ex);
            return null;
        }
    }

    private static bool IsRealPage(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<ExtractedDom?> ExtractDomAsync(CoreWebView2 core)
    {
        // Returns a JS object; ExecuteScriptAsync serialises it to JSON for us.
        const string script = @"
(function(){
  try {
    var sel = '';
    try { sel = (window.getSelection && window.getSelection().toString()) || ''; } catch(e) {}
    var text = '';
    try { text = (document.body && document.body.innerText) || ''; } catch(e) {}
    return {
      url: location.href,
      title: document.title || '',
      selection: sel,
      text: text
    };
  } catch (e) {
    return { url: location.href, title: document.title || '', selection: '', text: '' };
  }
})()";

        var json = await core.ExecuteScriptAsync(script);
        if (string.IsNullOrWhiteSpace(json) || json == "null") return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new ExtractedDom(
                Url: root.TryGetProperty("url", out var u) ? u.GetString() ?? string.Empty : string.Empty,
                Title: root.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty,
                Selection: root.TryGetProperty("selection", out var s) ? s.GetString() ?? string.Empty : string.Empty,
                Text: root.TryGetProperty("text", out var tx) ? tx.GetString() ?? string.Empty : string.Empty);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> CaptureScreenshotAsync(CoreWebView2 core)
    {
        try
        {
            using var ms = new MemoryStream();
            await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("PageContextService screenshot failed", ex);
            return null;
        }
    }

    private static string Truncate(string text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max) return text ?? string.Empty;
        // Middle-elide so both head and tail survive.
        var headLen = max / 2;
        var tailLen = max - headLen;
        return text.Substring(0, headLen) + "\n…[truncated]…\n" + text.Substring(text.Length - tailLen);
    }

    private static Task<T?> RunOnUIThreadAsync<T>(Func<Task<T?>> work)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            return work();
        }
        return dispatcher.InvokeAsync(work).Task.Unwrap();
    }

    private record ExtractedDom(string Url, string Title, string Selection, string Text);
}
