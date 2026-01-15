using System.Text.RegularExpressions;

namespace BrowserApp.Core.AdBlocker.Matching;

/// <summary>
/// Matches regular expressions against URLs.
/// Used for complex filter patterns (~5% of rules).
/// </summary>
public class RegexMatcher
{
    private readonly List<Regex> _regexPatterns = new();

    /// <summary>
    /// Adds a compiled regex pattern to the matcher.
    /// </summary>
    public void AddPattern(Regex regex)
    {
        _regexPatterns.Add(regex);
    }

    /// <summary>
    /// Checks if a URL matches any regex pattern.
    /// This is the slowest matching method, used as last resort.
    /// </summary>
    public bool IsMatch(string url)
    {
        foreach (var regex in _regexPatterns)
        {
            if (regex.IsMatch(url))
            {
                return true;
            }
        }

        return false;
    }

    public int Count => _regexPatterns.Count;
}
