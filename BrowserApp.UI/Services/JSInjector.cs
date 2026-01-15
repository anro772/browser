using System.Diagnostics;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for injecting JavaScript into web pages via WebView2.
/// Includes validation to detect dangerous patterns.
/// </summary>
public class JSInjector : IJSInjector, IDisposable
{
    private CoreWebView2? _coreWebView2;
    private bool _isDisposed;

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
            // Validate JavaScript doesn't contain extremely dangerous patterns
            ValidateJavaScript(js);

            // Log what's being injected for security audit
            ErrorLogger.LogInfo($"[JSInjector] Injecting JS ({js.Length} chars, timing: {timing})");

            string script;

            if (timing == "load")
            {
                // Wrap in window.onload event listener with error handling
                script = $@"
                (function() {{
                    if (document.readyState === 'complete') {{
                        try {{
                            {js}
                        }} catch (e) {{
                            console.error('[BrowserApp] Injection error (load):', e);
                        }}
                    }} else {{
                        window.addEventListener('load', function() {{
                            try {{
                                {js}
                            }} catch (e) {{
                                console.error('[BrowserApp] Injection error (load):', e);
                            }}
                        }});
                    }}
                }})();
                ";
            }
            else
            {
                // Execute immediately with error handling
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
            ErrorLogger.LogError("[JSInjector] Failed to inject JavaScript", ex);
        }
    }

    /// <summary>
    /// Validates JavaScript for extremely dangerous patterns.
    /// Note: This is NOT comprehensive security - JS injection is inherently risky.
    /// This only catches the most obvious malicious patterns.
    /// </summary>
    private void ValidateJavaScript(string js)
    {
        // Check for attempts to access file:// URLs
        if (Regex.IsMatch(js, @"file://", RegexOptions.IgnoreCase))
        {
            throw new SecurityException("JavaScript contains file:// URL access which is blocked");
        }

        // Check for attempts to execute external scripts from untrusted domains
        // This is just a basic check - not comprehensive
        if (Regex.IsMatch(js, @"\.src\s*=\s*[""'][^""']*(?<!https://cdn\.|https://www\.)[""']", RegexOptions.IgnoreCase))
        {
            // Log warning but don't block - might be legitimate
            Debug.WriteLine("[JSInjector] WARNING: Detected dynamic script loading");
        }

        // Check for potential eval/Function usage (warning only)
        if (Regex.IsMatch(js, @"\beval\s*\(", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(js, @"new\s+Function\s*\(", RegexOptions.IgnoreCase))
        {
            Debug.WriteLine("[JSInjector] WARNING: Detected eval() or Function() usage");
        }
    }

    public async Task InjectMultipleAsync(IEnumerable<string> jsSnippets)
    {
        foreach (var js in jsSnippets)
        {
            await InjectAsync(js);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        // Release reference to CoreWebView2 to allow cleanup
        _coreWebView2 = null;

        GC.SuppressFinalize(this);
    }
}
