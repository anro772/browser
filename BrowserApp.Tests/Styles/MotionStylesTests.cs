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
        var pattern = new Regex(
            "<Style\\s+x:Key=\"FadeInOnLoadModern\"[\\s\\S]*?<Setter\\s+Property=\"Opacity\"\\s+Value=\"0\"\\s*/>",
            RegexOptions.Compiled);

        Assert.DoesNotMatch(pattern, xaml);
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
