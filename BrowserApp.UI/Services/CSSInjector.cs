using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for injecting CSS into web pages via WebView2.
/// Includes XSS sanitization to prevent malicious code injection.
/// </summary>
public class CSSInjector : ICSSInjector, IDisposable
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
            // Sanitize CSS to prevent XSS attacks
            var sanitizedCss = SanitizeCss(css);

            // Escape for JavaScript template literal
            var escapedCss = sanitizedCss
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

    /// <summary>
    /// Sanitizes CSS to prevent XSS attacks.
    /// Removes dangerous patterns like &lt;/style&gt;, javascript: URLs, expression(), @import, etc.
    /// </summary>
    private string SanitizeCss(string css)
    {
        if (string.IsNullOrEmpty(css))
            return css;

        // Remove </style> tags that could break out of style context
        css = Regex.Replace(css, @"<\s*/\s*style\s*>", "", RegexOptions.IgnoreCase);

        // Remove <script> tags
        css = Regex.Replace(css, @"<\s*script[^>]*>.*?<\s*/\s*script\s*>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Remove javascript: URLs
        css = Regex.Replace(css, @"javascript\s*:", "", RegexOptions.IgnoreCase);

        // Remove vbscript: URLs
        css = Regex.Replace(css, @"vbscript\s*:", "", RegexOptions.IgnoreCase);

        // Remove data: URLs (can be used for XSS)
        css = Regex.Replace(css, @"data\s*:[^;]*;base64", "", RegexOptions.IgnoreCase);

        // Remove expression() (IE only but still dangerous)
        css = Regex.Replace(css, @"expression\s*\(", "", RegexOptions.IgnoreCase);

        // Remove -moz-binding (Firefox XSS vector)
        css = Regex.Replace(css, @"-moz-binding\s*:", "", RegexOptions.IgnoreCase);

        // Remove @import (can load external malicious CSS)
        css = Regex.Replace(css, @"@import", "", RegexOptions.IgnoreCase);

        // Remove behavior: (IE XSS vector)
        css = Regex.Replace(css, @"behavior\s*:", "", RegexOptions.IgnoreCase);

        return css;
    }

    public async Task InjectMultipleAsync(IEnumerable<string> cssRules)
    {
        foreach (var css in cssRules)
        {
            await InjectAsync(css);
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
