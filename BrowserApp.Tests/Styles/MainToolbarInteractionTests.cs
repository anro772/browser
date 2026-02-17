namespace BrowserApp.Tests.Styles;

public class MainToolbarInteractionTests
{
    [Fact]
    public void MainWindow_Toolbar_NoLongerDefinesLegacyManagerButtons()
    {
        string repoRoot = FindRepoRoot();
        string mainWindowPath = Path.Combine(repoRoot, "BrowserApp.UI", "MainWindow.xaml");
        string xaml = File.ReadAllText(mainWindowPath);

        Assert.DoesNotContain("x:Name=\"RulesButton\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"ChannelsButton\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"MarketplaceButton\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"ExtensionsButton\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"ProfileButton\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"SettingsButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ToolsButton\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_CodeBehind_UsesWorkspaceRoutingInsteadOfShowDialog()
    {
        string repoRoot = FindRepoRoot();
        string codePath = Path.Combine(repoRoot, "BrowserApp.UI", "MainWindow.xaml.cs");
        string code = File.ReadAllText(codePath);

        Assert.DoesNotContain("ShowDialog();", code, StringComparison.Ordinal);
        Assert.Contains("OpenWorkspace(WorkspaceSection.Rules);", code, StringComparison.Ordinal);
        Assert.Contains("OpenWorkspace(WorkspaceSection.Extensions);", code, StringComparison.Ordinal);
        Assert.Contains("OpenWorkspace(WorkspaceSection.Marketplace);", code, StringComparison.Ordinal);
        Assert.Contains("OpenWorkspace(WorkspaceSection.Channels);", code, StringComparison.Ordinal);
        Assert.Contains("OpenWorkspace(WorkspaceSection.Profiles);", code, StringComparison.Ordinal);
        Assert.Contains("OpenWorkspace(WorkspaceSection.Settings);", code, StringComparison.Ordinal);
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
