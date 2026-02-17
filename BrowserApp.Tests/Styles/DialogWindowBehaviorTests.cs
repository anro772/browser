using System.Text.RegularExpressions;

namespace BrowserApp.Tests.Styles;

public class DialogWindowBehaviorTests
{
    [Fact]
    public void ModernDialogWindowStyle_HidesOwnedDialogsFromTaskbar()
    {
        string repoRoot = FindRepoRoot();
        string stylesPath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "SurfaceStyles.xaml");
        string xaml = File.ReadAllText(stylesPath);

        var regex = new Regex(
            "<Style\\s+x:Key=\"ModernDialogWindowStyle\"[\\s\\S]*?<Setter\\s+Property=\"ShowInTaskbar\"\\s+Value=\"False\"\\s*/>",
            RegexOptions.Compiled);

        Assert.Matches(regex, xaml);
    }

    [Fact]
    public void ModernDialogWindowStyle_DoesNotSetWindowStartupLocation()
    {
        string repoRoot = FindRepoRoot();
        string stylesPath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "SurfaceStyles.xaml");
        string xaml = File.ReadAllText(stylesPath);

        var styleRegex = new Regex(
            "<Style\\s+x:Key=\"ModernDialogWindowStyle\"[\\s\\S]*?</Style>",
            RegexOptions.Compiled);

        Match styleMatch = styleRegex.Match(xaml);
        Assert.True(styleMatch.Success, "ModernDialogWindowStyle was not found.");
        Assert.DoesNotContain("Property=\"WindowStartupLocation\"", styleMatch.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void ManagerWindows_UseResizableSharedWindowStyle()
    {
        string repoRoot = FindRepoRoot();
        string[] managerWindows =
        {
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "SettingsView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "ExtensionManagerView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "RuleManagerView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "MarketplaceView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "ChannelsView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "ProfileSelectorView.xaml")
        };

        foreach (string windowPath in managerWindows)
        {
            string xaml = File.ReadAllText(windowPath);
            Match rootWindowTag = Regex.Match(xaml, "<ui:FluentWindow\\b[\\s\\S]*?>", RegexOptions.Compiled);

            Assert.Contains("Style=\"{StaticResource ModernManagerWindowStyle}\"", xaml);
            Assert.DoesNotContain("ResizeMode=\"NoResize\"", xaml);
            Assert.Contains("ShowMaximize=\"True\"", xaml);
            Assert.True(rootWindowTag.Success, $"Could not find root FluentWindow tag in {windowPath}.");
            Assert.Contains("WindowStartupLocation=\"CenterOwner\"", rootWindowTag.Value, StringComparison.Ordinal);
        }
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
