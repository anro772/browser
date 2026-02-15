using BrowserApp.Core.Models;
using BrowserApp.Core.Utilities;
using FluentAssertions;
using Xunit;

namespace BrowserApp.Tests.Models;

public class RuleTests : IDisposable
{
    public RuleTests()
    {
        UrlMatcher.ClearCache();
    }

    public void Dispose()
    {
        UrlMatcher.ClearCache();
    }

    // --- AppliesTo ---

    [Fact]
    public void AppliesTo_UniversalSite_MatchesAll()
    {
        var rule = new Rule { Site = "*" };

        var result = rule.AppliesTo("https://anything.com/whatever");

        result.Should().BeTrue();
    }

    [Fact]
    public void AppliesTo_EmptySite_MatchesAll()
    {
        var rule = new Rule { Site = "" };

        var result = rule.AppliesTo("https://anything.com/whatever");

        result.Should().BeTrue();
    }

    [Fact]
    public void AppliesTo_NullSite_MatchesAll()
    {
        var rule = new Rule { Site = null! };

        var result = rule.AppliesTo("https://anything.com/whatever");

        result.Should().BeTrue();
    }

    [Fact]
    public void AppliesTo_SpecificSite_MatchesPattern()
    {
        var rule = new Rule { Site = "*.example.com/*" };

        var result = rule.AppliesTo("https://sub.example.com/page");

        result.Should().BeTrue();
    }

    [Fact]
    public void AppliesTo_SpecificSite_RejectsNonMatching()
    {
        var rule = new Rule { Site = "*.example.com/*" };

        var result = rule.AppliesTo("https://other.com");

        result.Should().BeFalse();
    }

    [Fact]
    public void AppliesTo_EmptyUrl_ReturnsFalse()
    {
        var rule = new Rule { Site = "*.example.com/*" };

        var result = rule.AppliesTo("");

        result.Should().BeFalse();
    }

    // --- GetBlockActions ---

    [Fact]
    public void GetBlockActions_ReturnsOnlyBlockType()
    {
        var rule = new Rule
        {
            Rules = new List<RuleAction>
            {
                new RuleAction { Type = "block" },
                new RuleAction { Type = "inject_css" },
                new RuleAction { Type = "block" }
            }
        };

        var blockActions = rule.GetBlockActions().ToList();

        blockActions.Should().HaveCount(2);
        blockActions.Should().AllSatisfy(a => a.Type.Should().Be("block"));
    }

    [Fact]
    public void GetBlockActions_EmptyRules_ReturnsEmpty()
    {
        var rule = new Rule { Rules = new List<RuleAction>() };

        var blockActions = rule.GetBlockActions().ToList();

        blockActions.Should().BeEmpty();
    }

    // --- GetCssInjections ---

    [Fact]
    public void GetCssInjections_ReturnsOnlyInjectCssType()
    {
        var rule = new Rule
        {
            Rules = new List<RuleAction>
            {
                new RuleAction { Type = "inject_css", Css = "body { display: none; }" },
                new RuleAction { Type = "block" },
                new RuleAction { Type = "inject_js" }
            }
        };

        var cssInjections = rule.GetCssInjections().ToList();

        cssInjections.Should().HaveCount(1);
        cssInjections[0].Type.Should().Be("inject_css");
    }

    // --- GetJsInjections ---

    [Fact]
    public void GetJsInjections_ReturnsOnlyInjectJsType()
    {
        var rule = new Rule
        {
            Rules = new List<RuleAction>
            {
                new RuleAction { Type = "inject_js", Js = "console.log('hi');" },
                new RuleAction { Type = "block" },
                new RuleAction { Type = "inject_css" }
            }
        };

        var jsInjections = rule.GetJsInjections().ToList();

        jsInjections.Should().HaveCount(1);
        jsInjections[0].Type.Should().Be("inject_js");
    }

    // --- GetBlockActions with mixed types ---

    [Fact]
    public void GetBlockActions_MixedTypes_FiltersCorrectly()
    {
        var rule = new Rule
        {
            Rules = new List<RuleAction>
            {
                new RuleAction { Type = "block" },
                new RuleAction { Type = "inject_css" },
                new RuleAction { Type = "inject_js" },
                new RuleAction { Type = "block" },
                new RuleAction { Type = "inject_css" }
            }
        };

        rule.GetBlockActions().Should().HaveCount(2);
        rule.GetCssInjections().Should().HaveCount(2);
        rule.GetJsInjections().Should().HaveCount(1);
    }

    // --- Default values ---

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var rule = new Rule();

        rule.Id.Should().NotBeNullOrEmpty();
        rule.Site.Should().Be("*");
        rule.Enabled.Should().BeTrue();
        rule.Priority.Should().Be(10);
        rule.Source.Should().Be("local");
        rule.Name.Should().Be(string.Empty);
        rule.Description.Should().Be(string.Empty);
        rule.ChannelId.Should().BeNull();
        rule.IsEnforced.Should().BeFalse();
    }

    [Fact]
    public void Rules_DefaultsToEmptyList()
    {
        var rule = new Rule();

        rule.Rules.Should().NotBeNull();
        rule.Rules.Should().BeEmpty();
    }

    // --- Property round-trip ---

    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var id = "test-id-123";
        var createdAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 6, 20, 14, 0, 0, DateTimeKind.Utc);

        var rule = new Rule
        {
            Id = id,
            Name = "Test Rule",
            Description = "A test rule",
            Site = "*.test.com/*",
            Enabled = false,
            Priority = 50,
            Source = "marketplace",
            ChannelId = "channel-abc",
            IsEnforced = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        rule.Id.Should().Be(id);
        rule.Name.Should().Be("Test Rule");
        rule.Description.Should().Be("A test rule");
        rule.Site.Should().Be("*.test.com/*");
        rule.Enabled.Should().BeFalse();
        rule.Priority.Should().Be(50);
        rule.Source.Should().Be("marketplace");
        rule.ChannelId.Should().Be("channel-abc");
        rule.IsEnforced.Should().BeTrue();
        rule.CreatedAt.Should().Be(createdAt);
        rule.UpdatedAt.Should().Be(updatedAt);
    }

    // --- CreatedAt defaults ---

    [Fact]
    public void CreatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var rule = new Rule();
        var after = DateTime.UtcNow;

        rule.CreatedAt.Should().BeOnOrAfter(before);
        rule.CreatedAt.Should().BeOnOrBefore(after);
    }
}
