namespace BrowserApp.UI.Models;

/// <summary>
/// A canned prompt suggestion shown in the Copilot empty-state.
/// <see cref="Glyph"/> is the WPF UI fluent symbol name (e.g. "Document24").
/// </summary>
public class SuggestedPrompt
{
    public string Label { get; init; } = string.Empty;
    public string Glyph { get; init; } = "Sparkle24";
    public string PromptText { get; init; } = string.Empty;
}
