namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Service for injecting CSS into web pages.
/// </summary>
public interface ICSSInjector
{
    /// <summary>
    /// Injects CSS into the current page.
    /// </summary>
    /// <param name="css">The CSS to inject.</param>
    /// <param name="timing">When to inject: "dom_ready" (default) or "load".</param>
    Task InjectAsync(string css, string? timing = "dom_ready");

    /// <summary>
    /// Injects multiple CSS rules into the current page.
    /// </summary>
    /// <param name="cssRules">Collection of CSS strings to inject.</param>
    Task InjectMultipleAsync(IEnumerable<string> cssRules);
}
