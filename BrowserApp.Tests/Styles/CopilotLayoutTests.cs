using System.Text.RegularExpressions;

namespace BrowserApp.Tests.Styles;

public class CopilotLayoutTests
{
    [Fact]
    public void ModelSelector_HasNonClippingHeightAndCenteredContent()
    {
        string repoRoot = FindRepoRoot();
        string viewPath = Path.Combine(repoRoot, "BrowserApp.UI", "Views", "CopilotSidebarView.xaml");
        string xaml = File.ReadAllText(viewPath);

        // Ensure model selector has sufficient height and centered content for current font stack.
        var comboRegex = new Regex(
            "<ComboBox\\s+Grid.Column=\"1\"[\\s\\S]*?Height=\"(?<height>\\d+)\"[\\s\\S]*?VerticalContentAlignment=\"Center\"[\\s\\S]*?/>",
            RegexOptions.Compiled);

        var match = comboRegex.Match(xaml);
        Assert.True(match.Success, "Model selector ComboBox must define Height and VerticalContentAlignment.");

        int height = int.Parse(match.Groups["height"].Value);
        Assert.True(height >= 34, $"Expected model selector height >= 34, got {height}.");
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
