namespace BrowserApp.Tests.Styles;

public class WorkspaceViewResolutionTests
{
    [Fact]
    public void WorkspaceViews_ExistOnDisk()
    {
        string repoRoot = FindRepoRoot();
        string[] requiredFiles =
        {
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "Workspaces", "WorkspaceHostView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "Workspaces", "RulesWorkspaceView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "Workspaces", "ExtensionsWorkspaceView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "Workspaces", "MarketplaceWorkspaceView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "Workspaces", "ChannelsWorkspaceView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "Workspaces", "ProfilesWorkspaceView.xaml"),
            Path.Combine(repoRoot, "BrowserApp.UI", "Views", "Workspaces", "SettingsWorkspaceView.xaml")
        };

        foreach (string path in requiredFiles)
        {
            Assert.True(File.Exists(path), $"Missing workspace file: {path}");
        }
    }

    [Fact]
    public void AppServiceRegistration_IncludesWorkspaceViews()
    {
        string repoRoot = FindRepoRoot();
        string appCodePath = Path.Combine(repoRoot, "BrowserApp.UI", "App.xaml.cs");
        string code = File.ReadAllText(appCodePath);

        Assert.Contains("services.AddSingleton<WorkspaceHostView>();", code, StringComparison.Ordinal);
        Assert.Contains("services.AddTransient<RulesWorkspaceView>();", code, StringComparison.Ordinal);
        Assert.Contains("services.AddTransient<ExtensionsWorkspaceView>();", code, StringComparison.Ordinal);
        Assert.Contains("services.AddTransient<MarketplaceWorkspaceView>();", code, StringComparison.Ordinal);
        Assert.Contains("services.AddTransient<ChannelsWorkspaceView>();", code, StringComparison.Ordinal);
        Assert.Contains("services.AddTransient<ProfilesWorkspaceView>();", code, StringComparison.Ordinal);
        Assert.Contains("services.AddTransient<SettingsWorkspaceView>();", code, StringComparison.Ordinal);
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
