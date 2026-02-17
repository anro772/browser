namespace BrowserApp.Tests.Styles;

public class BrowserThemePaletteTests
{
    [Fact]
    public void BrowserTheme_UsesUpdatedNonPurpleAccentPalette()
    {
        string repoRoot = FindRepoRoot();
        string themePath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "BrowserTheme.xaml");
        string xaml = File.ReadAllText(themePath);

        Assert.Contains("Color x:Key=\"Accent\">#3B82F6</Color>", xaml, StringComparison.Ordinal);
        Assert.Contains("Color x:Key=\"AccentLight\">#60A5FA</Color>", xaml, StringComparison.Ordinal);
        Assert.Contains("Color x:Key=\"AccentDark\">#2563EB</Color>", xaml, StringComparison.Ordinal);

        Assert.DoesNotContain("#9E6BFF", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#BB95FF", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#7951E8", xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BrowserTheme_UsesGraphiteDarkSurfaces()
    {
        string repoRoot = FindRepoRoot();
        string themePath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "BrowserTheme.xaml");
        string xaml = File.ReadAllText(themePath);

        Assert.Contains("Color x:Key=\"BgPrimary\">#0B0E12</Color>", xaml, StringComparison.Ordinal);
        Assert.Contains("Color x:Key=\"BgSurface\">#171D26</Color>", xaml, StringComparison.Ordinal);
        Assert.Contains("Color x:Key=\"BgSurfaceHover\">#1F2733</Color>", xaml, StringComparison.Ordinal);
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
