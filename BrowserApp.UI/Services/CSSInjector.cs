using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for injecting CSS into web pages via WebView2.
/// </summary>
public class CSSInjector : ICSSInjector
{
    private CoreWebView2? _coreWebView2;

    /// <summary>
    /// Sets the CoreWebView2 instance to use for injection.
    /// Must be called after WebView2 is initialized.
    /// </summary>
    public void SetCoreWebView2(CoreWebView2 coreWebView2)
    {
        _coreWebView2 = coreWebView2;
    }

    public async Task InjectAsync(string css, string? timing = "dom_ready")
    {
        if (_coreWebView2 == null)
        {
            Debug.WriteLine("[CSSInjector] CoreWebView2 not set");
            return;
        }

        if (string.IsNullOrEmpty(css))
            return;

        try
        {
            // Escape backticks and dollar signs to avoid JavaScript template literal issues
            var escapedCss = css
                .Replace("\\", "\\\\")
                .Replace("`", "\\`")
                .Replace("$", "\\$")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n");

            var script = $@"
            (function() {{
                const style = document.createElement('style');
                style.setAttribute('data-injected', 'browser-app');
                style.textContent = `{escapedCss}`;
                if (document.head) {{
                    document.head.appendChild(style);
                }} else {{
                    document.documentElement.appendChild(style);
                }}
            }})();
            ";

            await _coreWebView2.ExecuteScriptAsync(script);
            Debug.WriteLine($"[CSSInjector] Injected CSS ({css.Length} chars)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CSSInjector] Error injecting CSS: {ex.Message}");
        }
    }

    public async Task InjectMultipleAsync(IEnumerable<string> cssRules)
    {
        foreach (var css in cssRules)
        {
            await InjectAsync(css);
        }
    }
}
