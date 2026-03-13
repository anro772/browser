namespace BrowserApp.Tests.Styles;

public class MainWindowDragBehaviorTests
{
    [Fact]
    public void MainWindow_DefinesDedicatedDragRegion()
    {
        string repoRoot = FindRepoRoot();
        string mainWindowPath = Path.Combine(repoRoot, "BrowserApp.UI", "MainWindow.xaml");
        string xaml = File.ReadAllText(mainWindowPath);

        Assert.Contains("x:Name=\"TabStripGrid\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseLeftButtonDown=\"TabStripArea_PreviewMouseLeftButtonDown\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_CodeBehind_ImplementsDragHandler()
    {
        string repoRoot = FindRepoRoot();
        string codePath = Path.Combine(repoRoot, "BrowserApp.UI", "MainWindow.xaml.cs");
        string code = File.ReadAllText(codePath);

        Assert.Contains("private void TabStripArea_PreviewMouseLeftButtonDown", code, StringComparison.Ordinal);
        Assert.Contains("DragMove();", code, StringComparison.Ordinal);
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
