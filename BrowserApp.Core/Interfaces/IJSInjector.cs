namespace BrowserApp.Core.Interfaces;

/// <summary>
/// Service for injecting JavaScript into web pages.
/// </summary>
public interface IJSInjector
{
    /// <summary>
    /// Injects JavaScript into the current page.
    /// </summary>
    /// <param name="js">The JavaScript to inject.</param>
    /// <param name="timing">When to inject: "dom_ready" (default) or "load".</param>
    Task InjectAsync(string js, string? timing = "dom_ready");

    /// <summary>
    /// Injects multiple JavaScript snippets into the current page.
    /// </summary>
    /// <param name="jsSnippets">Collection of JavaScript strings to inject.</param>
    Task InjectMultipleAsync(IEnumerable<string> jsSnippets);
}
