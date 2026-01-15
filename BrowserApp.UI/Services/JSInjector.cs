using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for injecting JavaScript into web pages via WebView2.
/// </summary>
public class JSInjector : IJSInjector
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

    public async Task InjectAsync(string js, string? timing = "dom_ready")
    {
        if (_coreWebView2 == null)
        {
            Debug.WriteLine("[JSInjector] CoreWebView2 not set");
            return;
        }

        if (string.IsNullOrEmpty(js))
            return;

        try
        {
            string script;

            if (timing == "load")
            {
                // Wrap in window.onload event listener
                script = $@"
                (function() {{
                    if (document.readyState === 'complete') {{
                        {js}
                    }} else {{
                        window.addEventListener('load', function() {{
                            {js}
                        }});
                    }}
                }})();
                ";
            }
            else
            {
                // Execute immediately (DOMContentLoaded already fired)
                script = $@"
                (function() {{
                    try {{
                        {js}
                    }} catch (e) {{
                        console.error('[BrowserApp] Injection error:', e);
                    }}
                }})();
                ";
            }

            await _coreWebView2.ExecuteScriptAsync(script);
            Debug.WriteLine($"[JSInjector] Injected JS ({js.Length} chars, timing: {timing})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[JSInjector] Error injecting JS: {ex.Message}");
        }
    }

    public async Task InjectMultipleAsync(IEnumerable<string> jsSnippets)
    {
        foreach (var js in jsSnippets)
        {
            await InjectAsync(js);
        }
    }
}
