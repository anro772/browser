namespace BrowserApp.Tests.Styles;

public class WorkspaceHostLayoutTests
{
    [Fact]
    public void WorkspaceHost_UsesIconCloseButtonInHeader()
    {
        string repoRoot = FindRepoRoot();
        string hostPath = Path.Combine(repoRoot, "BrowserApp.UI", "Views", "Workspaces", "WorkspaceHostView.xaml");
        string xaml = File.ReadAllText(hostPath);

        Assert.Contains("x:Name=\"WorkspaceCloseButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"Close tools\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Symbol=\"Dismiss24\"", xaml, StringComparison.Ordinal);
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
