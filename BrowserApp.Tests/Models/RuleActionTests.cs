using BrowserApp.Core.Models;
using FluentAssertions;
using Xunit;

namespace BrowserApp.Tests.Models;

public class RuleActionTests
{
    // --- RuleAction defaults ---

    [Fact]
    public void RuleAction_DefaultType_IsBlock()
    {
        var action = new RuleAction();

        action.Type.Should().Be("block");
    }

    [Fact]
    public void RuleAction_DefaultMatch_IsNotNull()
    {
        var action = new RuleAction();

        action.Match.Should().NotBeNull();
    }

    [Fact]
    public void RuleAction_DefaultTiming_IsDomReady()
    {
        var action = new RuleAction();

        action.Timing.Should().Be("dom_ready");
    }

    [Fact]
    public void RuleAction_CssAndJs_DefaultToNull()
    {
        var action = new RuleAction();

        action.Css.Should().BeNull();
        action.Js.Should().BeNull();
    }

    // --- RuleMatch defaults ---

    [Fact]
    public void RuleMatch_AllProperties_DefaultToNull()
    {
        var match = new RuleMatch();

        match.UrlPattern.Should().BeNull();
        match.ResourceType.Should().BeNull();
        match.Method.Should().BeNull();
    }

    // --- RuleMatch property round-trip ---

    [Fact]
    public void RuleMatch_Properties_CanBeSet()
    {
        var match = new RuleMatch
        {
            UrlPattern = "*tracker.com/*",
            ResourceType = "script",
            Method = "GET"
        };

        match.UrlPattern.Should().Be("*tracker.com/*");
        match.ResourceType.Should().Be("script");
        match.Method.Should().Be("GET");
    }
}
