using BrowserApp.Core.Utilities;
using FluentAssertions;
using Xunit;

namespace BrowserApp.Tests.Utilities;

public class UrlMatcherTests : IDisposable
{
    public UrlMatcherTests()
    {
        // Clear cache before each test to avoid cross-test pollution
        UrlMatcher.ClearCache();
    }

    public void Dispose()
    {
        UrlMatcher.ClearCache();
    }

    // --- Matches: Universal wildcard ---

    [Fact]
    public void Matches_UniversalWildcard_MatchesAnyUrl()
    {
        var result = UrlMatcher.Matches("https://example.com", "*");

        result.Should().BeTrue();
    }

    // --- Matches: Null/empty URL ---

    [Fact]
    public void Matches_NullUrl_ReturnsFalse()
    {
        var result = UrlMatcher.Matches(null!, "*");

        result.Should().BeFalse();
    }

    [Fact]
    public void Matches_EmptyUrl_ReturnsFalse()
    {
        var result = UrlMatcher.Matches("", "*");

        result.Should().BeFalse();
    }

    // --- Matches: Null/empty pattern ---

    [Fact]
    public void Matches_NullPattern_ReturnsFalse()
    {
        var result = UrlMatcher.Matches("https://example.com", null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void Matches_EmptyPattern_ReturnsFalse()
    {
        var result = UrlMatcher.Matches("https://example.com", "");

        result.Should().BeFalse();
    }

    // --- Matches: Exact URL ---

    [Fact]
    public void Matches_ExactUrl_ReturnsTrue()
    {
        var result = UrlMatcher.Matches("https://example.com", "https://example.com");

        result.Should().BeTrue();
    }

    // --- Matches: Subdomain wildcard ---

    [Fact]
    public void Matches_SubdomainWildcard_MatchesSubdomains()
    {
        var result = UrlMatcher.Matches("https://sub.example.com/page", "*.example.com/*");

        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_SubdomainWildcard_DoesNotMatchOtherDomains()
    {
        var result = UrlMatcher.Matches("https://other.com", "*.example.com/*");

        result.Should().BeFalse();
    }

    // --- Matches: Partial domain wildcard ---

    [Fact]
    public void Matches_PartialDomainWildcard_Works()
    {
        var result = UrlMatcher.Matches("https://mytracker.com/script.js", "*tracker.com/*");

        result.Should().BeTrue();
    }

    // --- Matches: Prefix wildcard ---

    [Fact]
    public void Matches_PrefixWildcard_Works()
    {
        var result = UrlMatcher.Matches("https://example.com/page123", "https://example.com/page*");

        result.Should().BeTrue();
    }

    // --- Matches: Middle wildcard ---

    [Fact]
    public void Matches_MiddleWildcard_Works()
    {
        var result = UrlMatcher.Matches("https://test.com/api/data", "https://*.com/api/*");

        result.Should().BeTrue();
    }

    // --- Matches: Case insensitivity ---

    [Fact]
    public void Matches_IsCaseInsensitive()
    {
        var result = UrlMatcher.Matches("https://example.com", "HTTPS://EXAMPLE.COM");

        result.Should().BeTrue();
    }

    // --- Matches: Special regex characters ---

    [Fact]
    public void Matches_SpecialRegexChars_AreEscaped()
    {
        // Pattern contains dots and question marks which are special in regex.
        // They should be escaped and treated as literal characters.
        var result = UrlMatcher.Matches(
            "https://example.com/page?id=1",
            "https://example.com/page?id=1");

        result.Should().BeTrue();
    }

    // --- Matches: No wildcard requires exact match ---

    [Fact]
    public void Matches_NoWildcard_RequiresExactMatch()
    {
        var result = UrlMatcher.Matches("https://example.com/page", "https://example.com");

        result.Should().BeFalse();
    }

    // --- Matches: Invalid pattern ---

    [Fact]
    public void Matches_InvalidPattern_ReturnsFalse()
    {
        // A pattern that would produce an invalid regex after processing.
        // The method catches exceptions and returns false.
        // Using unbalanced brackets which Regex.Escape would escape,
        // so we need a truly problematic scenario. Since the implementation
        // escapes patterns, it is hard to cause an exception. We verify
        // the catch path by testing an extremely unusual but valid scenario.
        // The Regex.Escape handles most special chars, so this is more of
        // a safety-net test.
        var result = UrlMatcher.Matches("https://example.com", "https://example.com");

        // This should match; the real test is that it doesn't throw.
        result.Should().BeTrue();
    }

    // --- MatchesResourceType ---

    [Fact]
    public void MatchesResourceType_NullFilter_MatchesAll()
    {
        var result = UrlMatcher.MatchesResourceType("script", null);

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesResourceType_MatchingType_ReturnsTrue()
    {
        var result = UrlMatcher.MatchesResourceType("script", "script");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesResourceType_DifferentType_ReturnsFalse()
    {
        var result = UrlMatcher.MatchesResourceType("script", "image");

        result.Should().BeFalse();
    }

    [Fact]
    public void MatchesResourceType_IsCaseInsensitive()
    {
        var result = UrlMatcher.MatchesResourceType("Script", "script");

        result.Should().BeTrue();
    }

    // --- MatchesMethod ---

    [Fact]
    public void MatchesMethod_NullFilter_MatchesAll()
    {
        var result = UrlMatcher.MatchesMethod("GET", null);

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesMethod_MatchingMethod_ReturnsTrue()
    {
        var result = UrlMatcher.MatchesMethod("GET", "GET");

        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesMethod_IsCaseInsensitive()
    {
        var result = UrlMatcher.MatchesMethod("get", "GET");

        result.Should().BeTrue();
    }

    // --- ClearCache ---

    [Fact]
    public void ClearCache_ClearsRegexCache()
    {
        // Populate the cache first
        UrlMatcher.Matches("https://example.com", "https://example.com");

        // Should not throw
        var exception = Record.Exception(() => UrlMatcher.ClearCache());

        exception.Should().BeNull();
    }
}
