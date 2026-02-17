using System.Text.RegularExpressions;

namespace BrowserApp.Tests.Styles;

public class SurfaceStylesTests
{
    [Fact]
    public void ModernDialogWindowStyle_MustPreserveBaseFluentWindowTemplate()
    {
        string repoRoot = FindRepoRoot();
        string stylesPath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "SurfaceStyles.xaml");
        string xaml = File.ReadAllText(stylesPath);

        // Explicit styles for FluentWindow must be based on the default FluentWindow style.
        var regex = new Regex(
            "<Style\\s+x:Key=\"ModernDialogWindowStyle\"\\s+TargetType=\"ui:FluentWindow\"\\s+BasedOn=\"\\{StaticResource\\s+\\{x:Type\\s+ui:FluentWindow\\}\\}\"",
            RegexOptions.Compiled);

        Assert.Matches(regex, xaml);
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
