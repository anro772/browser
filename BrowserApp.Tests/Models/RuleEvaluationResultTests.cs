using BrowserApp.Core.Models;
using FluentAssertions;
using Xunit;

namespace BrowserApp.Tests.Models;

public class RuleEvaluationResultTests
{
    // --- Allow ---

    [Fact]
    public void Allow_ReturnsShouldBlockFalse()
    {
        var result = RuleEvaluationResult.Allow();

        result.ShouldBlock.Should().BeFalse();
    }

    [Fact]
    public void Allow_HasNullBlockedByFields()
    {
        var result = RuleEvaluationResult.Allow();

        result.BlockedByRuleId.Should().BeNull();
        result.BlockedByRuleName.Should().BeNull();
    }

    [Fact]
    public void Allow_HasEmptyInjectionsList()
    {
        var result = RuleEvaluationResult.Allow();

        result.InjectionsToApply.Should().NotBeNull();
        result.InjectionsToApply.Should().BeEmpty();
    }

    // --- Block ---

    [Fact]
    public void Block_ReturnsShouldBlockTrue()
    {
        var result = RuleEvaluationResult.Block("rule-1", "Block Trackers");

        result.ShouldBlock.Should().BeTrue();
    }

    [Fact]
    public void Block_SetsRuleIdAndName()
    {
        var result = RuleEvaluationResult.Block("rule-42", "Ad Blocker");

        result.BlockedByRuleId.Should().Be("rule-42");
        result.BlockedByRuleName.Should().Be("Ad Blocker");
    }

    [Fact]
    public void Block_HasEmptyInjectionsList()
    {
        var result = RuleEvaluationResult.Block("rule-1", "Test");

        result.InjectionsToApply.Should().NotBeNull();
        result.InjectionsToApply.Should().BeEmpty();
    }

    // --- InjectionsToApply ---

    [Fact]
    public void InjectionsToApply_DefaultsToEmptyList()
    {
        var result = new RuleEvaluationResult();

        result.InjectionsToApply.Should().NotBeNull();
        result.InjectionsToApply.Should().BeEmpty();
    }

    [Fact]
    public void InjectionsToApply_CanBePopulated()
    {
        var injection = new RuleAction
        {
            Type = "inject_css",
            Css = "body { background: red; }"
        };

        var result = new RuleEvaluationResult
        {
            InjectionsToApply = new List<RuleAction> { injection }
        };

        result.InjectionsToApply.Should().HaveCount(1);
        result.InjectionsToApply[0].Type.Should().Be("inject_css");
        result.InjectionsToApply[0].Css.Should().Be("body { background: red; }");
    }
}
