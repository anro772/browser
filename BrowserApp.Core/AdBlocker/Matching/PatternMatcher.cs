using System.Text.RegularExpressions;

namespace BrowserApp.Core.AdBlocker.Matching;

/// <summary>
/// Matches wildcard patterns against URLs.
/// Converts wildcards to regex for matching.
/// </summary>
public class PatternMatcher
{
    private readonly List<PatternRule> _patterns = new();

    /// <summary>
    /// Adds a wildcard pattern to the matcher.
    /// </summary>
    public void AddPattern(string pattern)
    {
        // Convert wildcard pattern to regex
        // * = any characters
        // ^ = separator (anything except letter, digit, or one of: _ - . %)
        var regexPattern = ConvertWildcardToRegex(pattern);

        try
        {
            var regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _patterns.Add(new PatternRule
            {
                OriginalPattern = pattern,
                CompiledRegex = regex
            });
        }
        catch (Exception)
        {
            // Invalid pattern, skip
        }
    }

    /// <summary>
    /// Checks if a URL matches any pattern.
    /// </summary>
    public bool IsMatch(string url)
    {
        var lowerUrl = url.ToLowerInvariant();

        foreach (var pattern in _patterns)
        {
            if (pattern.CompiledRegex.IsMatch(lowerUrl))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts a wildcard pattern to a regex pattern.
    /// * = any characters
    /// ^ = separator (anything except letter, digit, or [-_.%])
    /// </summary>
    private string ConvertWildcardToRegex(string pattern)
    {
        // Escape regex special characters except * and ^
        var escaped = Regex.Escape(pattern)
            .Replace("\\*", ".*")  // * = any characters
            .Replace("\\^", @"[^\w\-\.\%]");  // ^ = separator

        // Anchor to start/end if pattern starts/ends with |
        if (pattern.StartsWith("|") && !pattern.StartsWith("||"))
        {
            escaped = "^" + escaped.Substring(1);
        }

        if (pattern.EndsWith("|"))
        {
            escaped = escaped.Substring(0, escaped.Length - 1) + "$";
        }

        return escaped;
    }

    public int Count => _patterns.Count;

    private class PatternRule
    {
        public string OriginalPattern { get; set; } = string.Empty;
        public Regex CompiledRegex { get; set; } = null!;
    }
}
