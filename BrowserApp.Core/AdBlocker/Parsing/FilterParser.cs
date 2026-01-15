using System.Diagnostics;
using System.Text.RegularExpressions;
using BrowserApp.Core.AdBlocker.Interfaces;
using BrowserApp.Core.AdBlocker.Models;

namespace BrowserApp.Core.AdBlocker.Parsing;

/// <summary>
/// Parses uBlock Origin / EasyList filter syntax into FilterRule objects.
/// Supports network filters (Phase 1 + 2) with basic options (Phase 3).
/// </summary>
public class FilterParser : IFilterParser
{
    private int _parsedCount = 0;
    private int _skippedCount = 0;
    private int _errorCount = 0;

    public FilterRule? ParseLine(string line)
    {
        try
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                _skippedCount++;
                return null;
            }

            line = line.Trim();

            // Skip comments
            if (line.StartsWith("!") || line.StartsWith("#"))
            {
                _skippedCount++;
                return null;
            }

            // Check if this is a cosmetic filter (##)
            if (line.Contains("##") || line.Contains("#@#"))
            {
                // Skip cosmetic filters for now (Phase 4)
                _skippedCount++;
                return null;
            }

            // Parse as network filter
            var rule = ParseNetworkFilter(line);

            if (rule != null)
            {
                _parsedCount++;
            }
            else
            {
                _skippedCount++;
            }

            return rule;
        }
        catch (Exception ex)
        {
            _errorCount++;
            Debug.WriteLine($"[FilterParser] Error parsing line: {line}");
            Debug.WriteLine($"[FilterParser] Error: {ex.Message}");
            return null;
        }
    }

    public IEnumerable<FilterRule> ParseFilterList(string filterListContent)
    {
        _parsedCount = 0;
        _skippedCount = 0;
        _errorCount = 0;

        var lines = filterListContent.Split('\n');
        var rules = new List<FilterRule>();

        foreach (var line in lines)
        {
            var rule = ParseLine(line);
            if (rule != null)
            {
                rules.Add(rule);
            }
        }

        Debug.WriteLine($"[FilterParser] Parsed {_parsedCount} rules, skipped {_skippedCount}, errors {_errorCount}");

        return rules;
    }

    /// <summary>
    /// Parses a network filter line.
    /// Examples:
    ///   ||doubleclick.net^
    ///   |https://ads.example.com/banner.js|
    ///   /ad-banner-\d+\.png/
    ///   @@||example.com^
    ///   ||ads.com^$script,third-party
    /// </summary>
    private FilterRule? ParseNetworkFilter(string line)
    {
        var rule = new FilterRule
        {
            Type = FilterType.Network,
            RawFilter = line
        };

        // Check for exception rule (@@)
        if (line.StartsWith("@@"))
        {
            rule.IsException = true;
            line = line.Substring(2);
        }

        // Split off options ($)
        var parts = line.Split('$', 2);
        var pattern = parts[0];
        var optionsString = parts.Length > 1 ? parts[1] : null;

        // Parse options if present
        if (!string.IsNullOrEmpty(optionsString))
        {
            rule.Options = ParseOptions(optionsString);
        }

        // Determine match type and extract pattern
        if (pattern.StartsWith("/") && pattern.EndsWith("/"))
        {
            // Regex pattern: /banner\d+\.png/
            rule.MatchType = Models.MatchType.Regex;
            var regexPattern = pattern.Substring(1, pattern.Length - 2);

            try
            {
                rule.CompiledRegex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                rule.Pattern = regexPattern;
            }
            catch (Exception)
            {
                // Invalid regex, skip this rule
                return null;
            }
        }
        else if (pattern.StartsWith("||") && pattern.EndsWith("^"))
        {
            // Exact domain: ||doubleclick.net^
            rule.MatchType = Models.MatchType.ExactDomain;
            rule.Pattern = pattern.Substring(2, pattern.Length - 3).ToLowerInvariant();
        }
        else if (pattern.StartsWith("|") && pattern.EndsWith("|"))
        {
            // Exact URL: |https://example.com/ad.js|
            rule.MatchType = Models.MatchType.ExactUrl;
            rule.Pattern = pattern.Substring(1, pattern.Length - 2).ToLowerInvariant();
        }
        else if (pattern.Contains("*") || pattern.Contains("^"))
        {
            // Wildcard pattern: ad-banner-*.png or example.com^
            rule.MatchType = Models.MatchType.Wildcard;
            rule.Pattern = pattern.ToLowerInvariant();
        }
        else
        {
            // Simple substring match (treat as wildcard)
            rule.MatchType = Models.MatchType.Wildcard;
            rule.Pattern = pattern.ToLowerInvariant();
        }

        return rule;
    }

    /// <summary>
    /// Parses filter options (e.g., "script,third-party,domain=example.com").
    /// </summary>
    private FilterOptions ParseOptions(string optionsString)
    {
        var options = new FilterOptions();
        var parts = optionsString.Split(',');

        foreach (var part in parts)
        {
            var option = part.Trim();

            if (option == "third-party")
            {
                options.ThirdPartyOnly = true;
            }
            else if (option == "~third-party")
            {
                options.ThirdPartyOnly = false;
            }
            else if (option.StartsWith("domain="))
            {
                var domains = option.Substring(7).Split('|');
                foreach (var domain in domains)
                {
                    if (domain.StartsWith("~"))
                    {
                        options.ExcludedDomains.Add(domain.Substring(1).ToLowerInvariant());
                    }
                    else
                    {
                        options.ApplicableDomains.Add(domain.ToLowerInvariant());
                    }
                }
            }
            else if (option.StartsWith("~"))
            {
                // Blocked resource type
                options.BlockedResourceTypes.Add(option.Substring(1).ToLowerInvariant());
            }
            else
            {
                // Allowed resource type
                options.AllowedResourceTypes.Add(option.ToLowerInvariant());
            }
        }

        return options;
    }
}
