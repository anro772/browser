using System.Text.RegularExpressions;

namespace BrowserApp.Tests.Styles;

public class MotionStylesTests
{
    [Fact]
    public void FadeInOnLoadModern_DoesNotForcePermanentZeroOpacity()
    {
        string repoRoot = FindRepoRoot();
        string motionStylesPath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "MotionStyles.xaml");
        string xaml = File.ReadAllText(motionStylesPath);

        // The fade style must not default controls to Opacity=0 because that can render blank dialogs
        // if Loaded animation timing doesn't run as expected.
        var styleRegex = new Regex(
            "<Style\\s+x:Key=\"FadeInOnLoadModern\"[\\s\\S]*?</Style>",
            RegexOptions.Compiled);

        Match styleMatch = styleRegex.Match(xaml);
        Assert.True(styleMatch.Success, "FadeInOnLoadModern style was not found.");
        Assert.DoesNotContain("Property=\"Opacity\" Value=\"0\"", styleMatch.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceOverlayAnimatedStyle_DefinesFadeSlideAndScaleTransitions()
    {
        string repoRoot = FindRepoRoot();
        string motionStylesPath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "MotionStyles.xaml");
        string xaml = File.ReadAllText(motionStylesPath);

        var styleRegex = new Regex(
            "<Style\\s+x:Key=\"WorkspaceOverlayAnimatedStyle\"[\\s\\S]*?</Style>",
            RegexOptions.Compiled);

        Match styleMatch = styleRegex.Match(xaml);
        Assert.True(styleMatch.Success, "WorkspaceOverlayAnimatedStyle was not found.");
        Assert.Contains("Duration=\"0:0:0.2\"", styleMatch.Value, StringComparison.Ordinal);
        Assert.Contains("PremiumEaseOut", styleMatch.Value, StringComparison.Ordinal);
        Assert.Contains("(ScaleTransform.ScaleX)", styleMatch.Value, StringComparison.Ordinal);
        Assert.Contains("(ScaleTransform.ScaleY)", styleMatch.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_UsesWorkspaceOverlayAnimatedStyle()
    {
        string repoRoot = FindRepoRoot();
        string mainWindowPath = Path.Combine(repoRoot, "BrowserApp.UI", "MainWindow.xaml");
        string xaml = File.ReadAllText(mainWindowPath);

        Assert.Contains("x:Name=\"WorkspaceOverlay\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource WorkspaceOverlayAnimatedStyle}\"", xaml, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "BrowserApp.UI")) &&
                Directory.Exists(Path.Combine(dir.FullName, "BrowserApp.Tests")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
