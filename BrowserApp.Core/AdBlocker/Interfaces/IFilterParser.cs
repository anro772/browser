using BrowserApp.Core.AdBlocker.Models;

namespace BrowserApp.Core.AdBlocker.Interfaces;

/// <summary>
/// Parses uBlock Origin / EasyList filter syntax into structured rules.
/// </summary>
public interface IFilterParser
{
    /// <summary>
    /// Parses a single filter line into a FilterRule.
    /// </summary>
    /// <param name="line">The filter line to parse</param>
    /// <returns>Parsed filter rule, or null if invalid/comment</returns>
    FilterRule? ParseLine(string line);

    /// <summary>
    /// Parses an entire filter list into rules.
    /// </summary>
    /// <param name="filterListContent">Raw filter list content</param>
    /// <returns>Collection of parsed filter rules</returns>
    IEnumerable<FilterRule> ParseFilterList(string filterListContent);
}
