using System.Reflection;
using BrowserApp.UI.Services;
using FluentAssertions;
using Xunit;

namespace BrowserApp.Tests.Services;

/// <summary>
/// Tests for FilterListService parser, ShouldBlock, and GetCosmeticCss logic.
/// Uses reflection to call the private ParseFilterLine method directly,
/// avoiding HTTP downloads and file system dependencies.
/// </summary>
public class FilterListServiceTests : IDisposable
{
    private readonly FilterListService _sut;
    private readonly MethodInfo _parseFilterLine;
    private readonly FieldInfo _isLoadedField;

    public FilterListServiceTests()
    {
        _sut = new FilterListService();

        // Access private ParseFilterLine method for direct testing
        _parseFilterLine = typeof(FilterListService)
            .GetMethod("ParseFilterLine", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Access private _isLoaded field to enable ShouldBlock/GetCosmeticCss
        _isLoadedField = typeof(FilterListService)
            .GetField("_isLoaded", BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    private bool ParseLine(string line)
    {
        return (bool)_parseFilterLine.Invoke(_sut, new object[] { line })!;
    }

    private void SetLoaded()
    {
        _isLoadedField.SetValue(_sut, true);
    }

    // --- Parser Tests ---

    [Fact]
    public void ParseFilterLine_DomainBlock_ParsesSuccessfully()
    {
        ParseLine("||ads.example.com^").Should().BeTrue();
        SetLoaded();

        _sut.ShouldBlock("https://ads.example.com/banner.js", "https://example.com", "script")
            .Should().BeTrue();
    }

    [Fact]
    public void ParseFilterLine_DomainBlock_BlocksSubdomains()
    {
        ParseLine("||tracker.com^").Should().BeTrue();
        SetLoaded();

        _sut.ShouldBlock("https://sub.tracker.com/pixel.gif", "https://example.com", "image")
            .Should().BeTrue();
    }

    [Fact]
    public void ParseFilterLine_WhitelistException_AllowsDomain()
    {
        ParseLine("||example.com^").Should().BeTrue();
        ParseLine("@@||example.com^").Should().BeTrue();
        SetLoaded();

        _sut.ShouldBlock("https://example.com/page.html", null, "document")
            .Should().BeFalse("whitelisted domain should not be blocked");
    }

    [Fact]
    public void ParseFilterLine_GlobalCosmeticSelector_ParsesSuccessfully()
    {
        ParseLine("##.ad-banner").Should().BeTrue();
        SetLoaded();

        var css = _sut.GetCosmeticCss("https://example.com");
        css.Should().NotBeNull();
        css.Should().Contain(".ad-banner");
        css.Should().Contain("display: none !important");
    }

    [Fact]
    public void ParseFilterLine_DomainSpecificCosmeticSelector_ParsesSuccessfully()
    {
        ParseLine("example.com##.sidebar-ad").Should().BeTrue();
        SetLoaded();

        // Should match for the specified domain
        var css = _sut.GetCosmeticCss("https://example.com/page");
        css.Should().NotBeNull();
        css.Should().Contain(".sidebar-ad");

        // Should NOT match for other domains
        var otherCss = _sut.GetCosmeticCss("https://other.com/page");
        otherCss.Should().BeNull();
    }

    [Fact]
    public void ParseFilterLine_MultiDomainCosmeticSelector_ParsesSuccessfully()
    {
        ParseLine("site1.com,site2.com##.popup-ad").Should().BeTrue();
        SetLoaded();

        _sut.GetCosmeticCss("https://site1.com").Should().Contain(".popup-ad");
        _sut.GetCosmeticCss("https://site2.com").Should().Contain(".popup-ad");
        _sut.GetCosmeticCss("https://site3.com").Should().BeNull();
    }

    [Fact]
    public void ParseFilterLine_UrlPathPattern_Blocks()
    {
        ParseLine("||cdn.example.com/ads/").Should().BeTrue();
        SetLoaded();

        _sut.ShouldBlock("https://cdn.example.com/ads/banner.js", null, "script")
            .Should().BeTrue();
    }

    [Fact]
    public void ParseFilterLine_WildcardPattern_Blocks()
    {
        ParseLine("*/ads/banner*.js").Should().BeTrue();
        SetLoaded();

        _sut.ShouldBlock("https://cdn.example.com/ads/banner123.js", null, "script")
            .Should().BeTrue();
    }

    [Fact]
    public void ParseFilterLine_ProceduralCosmetic_Skipped()
    {
        ParseLine("example.com##.ad:has(.tracking)").Should().BeFalse(
            "procedural cosmetic filters should be skipped");
    }

    [Fact]
    public void ParseFilterLine_CommentLine_Skipped()
    {
        // Comments start with ! — they should be handled before ParseFilterLine is called,
        // but the method should handle empty/trivial inputs gracefully
        ParseLine("").Should().BeFalse();
    }

    [Fact]
    public void ParseFilterLine_UnsupportedModifiers_Skipped()
    {
        ParseLine("||example.com^$popup").Should().BeFalse(
            "popup modifier should be skipped");
        ParseLine("||example.com^$redirect=noop").Should().BeFalse(
            "redirect modifier should be skipped");
    }

    [Fact]
    public void ParseFilterLine_DomainBlockWithModifiers_Parses()
    {
        ParseLine("||tracker.com^$third-party").Should().BeTrue(
            "third-party modifier should be supported");
        SetLoaded();

        _sut.ShouldBlock("https://tracker.com/script.js", null, "script")
            .Should().BeTrue();
    }

    // --- ShouldBlock Tests ---

    [Fact]
    public void ShouldBlock_WhenNotLoaded_ReturnsFalse()
    {
        // Don't call SetLoaded
        _sut.ShouldBlock("https://ads.example.com", null, "script").Should().BeFalse();
    }

    [Fact]
    public void ShouldBlock_EmptyUrl_ReturnsFalse()
    {
        SetLoaded();
        _sut.ShouldBlock("", null, "script").Should().BeFalse();
    }

    [Fact]
    public void ShouldBlock_InvalidUrl_ReturnsFalse()
    {
        SetLoaded();
        _sut.ShouldBlock("not-a-url", null, "script").Should().BeFalse();
    }

    [Fact]
    public void ShouldBlock_CleanUrl_ReturnsFalse()
    {
        ParseLine("||ads.tracker.com^").Should().BeTrue();
        SetLoaded();

        _sut.ShouldBlock("https://clean-site.com/page.html", null, "document")
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldBlock_ExactDomainMatch_ReturnsTrue()
    {
        ParseLine("||doubleclick.net^").Should().BeTrue();
        SetLoaded();

        _sut.ShouldBlock("https://doubleclick.net/ad.js", null, "script")
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldBlock_WhitelistOverridesBlock_ReturnsFalse()
    {
        ParseLine("||cdn.example.com^").Should().BeTrue();
        ParseLine("@@||cdn.example.com^").Should().BeTrue();
        SetLoaded();

        _sut.ShouldBlock("https://cdn.example.com/lib.js", null, "script")
            .Should().BeFalse();
    }

    // --- GetCosmeticCss Tests ---

    [Fact]
    public void GetCosmeticCss_WhenNotLoaded_ReturnsNull()
    {
        _sut.GetCosmeticCss("https://example.com").Should().BeNull();
    }

    [Fact]
    public void GetCosmeticCss_EmptyUrl_ReturnsNull()
    {
        SetLoaded();
        _sut.GetCosmeticCss("").Should().BeNull();
    }

    [Fact]
    public void GetCosmeticCss_InvalidUrl_ReturnsNull()
    {
        SetLoaded();
        _sut.GetCosmeticCss("not-a-url").Should().BeNull();
    }

    [Fact]
    public void GetCosmeticCss_CombinesGlobalAndDomainSelectors()
    {
        ParseLine("##.global-ad").Should().BeTrue();
        ParseLine("example.com##.site-specific-ad").Should().BeTrue();
        SetLoaded();

        var css = _sut.GetCosmeticCss("https://example.com/page");
        css.Should().NotBeNull();
        css.Should().Contain(".global-ad");
        css.Should().Contain(".site-specific-ad");
    }

    [Fact]
    public void GetCosmeticCss_MatchesParentDomain()
    {
        ParseLine("example.com##.ad-widget").Should().BeTrue();
        SetLoaded();

        // www.example.com should match parent domain example.com
        var css = _sut.GetCosmeticCss("https://www.example.com/page");
        css.Should().NotBeNull();
        css.Should().Contain(".ad-widget");
    }

    [Fact]
    public void GetCosmeticCss_NoMatchingSelectors_ReturnsNull()
    {
        ParseLine("specific-site.com##.ad").Should().BeTrue();
        SetLoaded();

        _sut.GetCosmeticCss("https://other-site.com").Should().BeNull();
    }

    [Fact]
    public void GetCosmeticCss_DeduplicatesSelectors()
    {
        ParseLine("##.duplicate-ad").Should().BeTrue();
        ParseLine("##.duplicate-ad").Should().BeTrue();
        SetLoaded();

        var css = _sut.GetCosmeticCss("https://example.com");
        css.Should().NotBeNull();

        // Should contain the selector but the Distinct() call should prevent exact duplicates
        // in the joined output
        var selectorCount = css!.Split(".duplicate-ad").Length - 1;
        selectorCount.Should().Be(1, "duplicate selectors should be deduplicated");
    }

    // --- GetTotalFilterCount Tests ---

    [Fact]
    public void GetTotalFilterCount_InitiallyZero()
    {
        _sut.GetTotalFilterCount().Should().Be(0);
    }

    [Fact]
    public void GetTotalFilterCount_IncrementsAfterParsing()
    {
        ParseLine("||ads.com^").Should().BeTrue();
        ParseLine("##.ad-banner").Should().BeTrue();
        ParseLine("example.com##.sidebar").Should().BeTrue();

        _sut.GetTotalFilterCount().Should().Be(3);
    }

    // --- GetLists Tests ---

    [Fact]
    public void GetLists_ReturnsEasyListAndEasyPrivacy()
    {
        var lists = _sut.GetLists();
        lists.Should().HaveCount(2);
        lists.Should().Contain(l => l.Id == "easylist");
        lists.Should().Contain(l => l.Id == "easyprivacy");
    }

    public void Dispose()
    {
        _sut.Dispose();
    }
}
