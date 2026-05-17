using BrowserApp.UI.Views;
using Wpf.Ui.Controls;
using Xunit;

namespace BrowserApp.Tests.Views;

/// <summary>
/// Pure-function tests for the parsed-RulesJson summary bullets that the
/// MarketplaceRuleDetailDialog shows under "WHAT IT DOES". The dialog itself
/// needs an STA host to test, but BuildBullets is a pure static method.
/// </summary>
public class MarketplaceRuleDetailDialog_BuildBulletsTests
{
    [Fact]
    public void Empty_ReturnsSingleNoActionsBullet()
    {
        var bullets = MarketplaceRuleDetailDialog.BuildBullets("");

        Assert.Single(bullets);
        Assert.Contains("no actions", bullets[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Whitespace_ReturnsSingleNoActionsBullet()
    {
        var bullets = MarketplaceRuleDetailDialog.BuildBullets("   ");
        Assert.Single(bullets);
    }

    [Fact]
    public void Malformed_Json_ReturnsWarning()
    {
        var bullets = MarketplaceRuleDetailDialog.BuildBullets("{not json at all");
        Assert.Single(bullets);
        Assert.Contains("Could not parse", bullets[0].Text);
    }

    [Fact]
    public void RulesJson_AsArrayOfActions_ParsesCorrectly()
    {
        // Server stores RulesJson as a JSON-encoded array, not a full Rule shape.
        // The parser must handle this case via the fallback deserialization.
        var json = "[{\"type\":\"block\",\"match\":{\"urlPattern\":\"*tracker*\"}}]";

        var bullets = MarketplaceRuleDetailDialog.BuildBullets(json);

        Assert.Single(bullets);
        Assert.Contains("Blocks 1", bullets[0].Text);
        Assert.Contains("*tracker*", bullets[0].Text);
    }

    [Fact]
    public void RulesJson_AsFullRule_ParsesCorrectly()
    {
        // The "full Rule" shape (with Name, Site, Rules array) — older format.
        var json = "{\"name\":\"Test\",\"site\":\"*\",\"rules\":[{\"type\":\"block\",\"match\":{\"urlPattern\":\"*ads*\"}}]}";

        var bullets = MarketplaceRuleDetailDialog.BuildBullets(json);

        Assert.Single(bullets);
        Assert.Contains("Blocks 1", bullets[0].Text);
    }

    [Fact]
    public void Block_ShowsCount()
    {
        var json = "[{\"type\":\"block\",\"match\":{\"urlPattern\":\"*a*\"}}," +
                   "{\"type\":\"block\",\"match\":{\"urlPattern\":\"*b*\"}}," +
                   "{\"type\":\"block\",\"match\":{\"urlPattern\":\"*c*\"}}]";

        var bullets = MarketplaceRuleDetailDialog.BuildBullets(json);

        Assert.Single(bullets);
        Assert.Contains("Blocks 3", bullets[0].Text);
    }

    [Fact]
    public void Block_OverThreePatterns_TruncatesToFirstThreeWithEllipsis()
    {
        var json = "[{\"type\":\"block\",\"match\":{\"urlPattern\":\"*p1*\"}}," +
                   "{\"type\":\"block\",\"match\":{\"urlPattern\":\"*p2*\"}}," +
                   "{\"type\":\"block\",\"match\":{\"urlPattern\":\"*p3*\"}}," +
                   "{\"type\":\"block\",\"match\":{\"urlPattern\":\"*p4*\"}}," +
                   "{\"type\":\"block\",\"match\":{\"urlPattern\":\"*p5*\"}}]";

        var bullets = MarketplaceRuleDetailDialog.BuildBullets(json);

        Assert.Contains("*p1*", bullets[0].Text);
        Assert.Contains("*p2*", bullets[0].Text);
        Assert.Contains("*p3*", bullets[0].Text);
        Assert.DoesNotContain("*p4*", bullets[0].Text);
        Assert.Contains("…", bullets[0].Text); // unicode ellipsis
    }

    [Fact]
    public void CssInjection_ShowsCount()
    {
        var json = "[{\"type\":\"inject_css\",\"css\":\".banner{display:none}\"}," +
                   "{\"type\":\"inject_css\",\"css\":\".sidebar{display:none}\"}]";

        var bullets = MarketplaceRuleDetailDialog.BuildBullets(json);

        Assert.Single(bullets);
        Assert.Contains("Injects 2 CSS", bullets[0].Text);
    }

    [Fact]
    public void JsInjection_IncludesTimingLabel()
    {
        var json = "[{\"type\":\"inject_js\",\"timing\":\"dom_ready\",\"js\":\"console.log(1)\"}]";

        var bullets = MarketplaceRuleDetailDialog.BuildBullets(json);

        Assert.Single(bullets);
        Assert.Contains("Runs 1", bullets[0].Text);
        Assert.Contains("dom_ready", bullets[0].Text);
    }

    [Fact]
    public void HeaderModification_GetsItsOwnBullet()
    {
        var json = "[{\"type\":\"modify_headers\",\"headers\":[]}]";

        var bullets = MarketplaceRuleDetailDialog.BuildBullets(json);

        Assert.Single(bullets);
        Assert.Contains("Modifies HTTP headers", bullets[0].Text);
    }

    [Fact]
    public void MixedActions_ProducesOneBulletPerType()
    {
        var json = "[{\"type\":\"block\",\"match\":{\"urlPattern\":\"*ads*\"}}," +
                   "{\"type\":\"inject_css\",\"css\":\".x{}\"}," +
                   "{\"type\":\"inject_js\",\"timing\":\"load\",\"js\":\"x\"}]";

        var bullets = MarketplaceRuleDetailDialog.BuildBullets(json);

        Assert.Equal(3, bullets.Count);
        Assert.Contains(bullets, b => b.Text.StartsWith("Blocks"));
        Assert.Contains(bullets, b => b.Text.StartsWith("Injects 1 CSS"));
        Assert.Contains(bullets, b => b.Text.StartsWith("Runs 1"));
    }

    [Fact]
    public void UnknownActionType_FallsBackToGenericCustomBullet()
    {
        // An action type we don't have a friendly bullet for shouldn't be silently
        // dropped — the user should see *something* so they know there's content
        // and can dig into the raw JSON expander.
        var json = "[{\"type\":\"future_action\",\"match\":{}}]";

        var bullets = MarketplaceRuleDetailDialog.BuildBullets(json);

        Assert.Single(bullets);
        Assert.Contains("custom action", bullets[0].Text);
    }

    [Fact]
    public void Pluralization_HandlesSingular()
    {
        var json = "[{\"type\":\"block\",\"match\":{\"urlPattern\":\"*x*\"}}]";
        var bullets = MarketplaceRuleDetailDialog.BuildBullets(json);

        // "1 URL pattern" — no trailing 's'
        Assert.DoesNotContain("patterns", bullets[0].Text);
        Assert.Contains("pattern", bullets[0].Text);
    }
}

public class ActionBulletRecordTests
{
    [Fact]
    public void RecordEquality()
    {
        var a = new ActionBullet(SymbolRegular.Info24, System.Windows.Media.Brushes.Red, "msg");
        var b = new ActionBullet(SymbolRegular.Info24, System.Windows.Media.Brushes.Red, "msg");
        Assert.Equal(a, b);
    }
}
