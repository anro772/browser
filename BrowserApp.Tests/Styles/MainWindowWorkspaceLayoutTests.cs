namespace BrowserApp.Tests.Styles;

public class MainWindowWorkspaceLayoutTests
{
    [Fact]
    public void MainWindow_HasWorkspaceOverlayAndHostContainer()
    {
        string repoRoot = FindRepoRoot();
        string mainWindowPath = Path.Combine(repoRoot, "BrowserApp.UI", "MainWindow.xaml");
        string xaml = File.ReadAllText(mainWindowPath);

        Assert.Contains("x:Name=\"WorkspaceOverlay\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"WorkspaceHostContainer\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{Binding IsWorkspaceOpen", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_CodeBehind_WiresWorkspaceHost()
    {
        string repoRoot = FindRepoRoot();
        string codePath = Path.Combine(repoRoot, "BrowserApp.UI", "MainWindow.xaml.cs");
        string code = File.ReadAllText(codePath);

        Assert.Contains("WorkspaceHostContainer.Content = _workspaceHostView;", code, StringComparison.Ordinal);
        Assert.Contains("_workspaceHostView.SetWorkspaceContent(", code, StringComparison.Ordinal);
        Assert.Contains("_viewModel.PropertyChanged += OnMainViewModelPropertyChanged;", code, StringComparison.Ordinal);
        Assert.Contains("if (_viewModel.IsWorkspaceOpen)", code, StringComparison.Ordinal);
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
