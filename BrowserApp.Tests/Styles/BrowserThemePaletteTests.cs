namespace BrowserApp.Tests.Styles;

public class BrowserThemePaletteTests
{
    [Fact]
    public void BrowserTheme_UsesWarmObsidianAccentPalette()
    {
        string repoRoot = FindRepoRoot();
        string themePath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "BrowserTheme.xaml");
        string xaml = File.ReadAllText(themePath);

        Assert.Contains("Color x:Key=\"Accent\">#7C6AEF</Color>", xaml, StringComparison.Ordinal);
        Assert.Contains("Color x:Key=\"AccentLight\">#9B8AFB</Color>", xaml, StringComparison.Ordinal);
        Assert.Contains("Color x:Key=\"AccentDark\">#6254CC</Color>", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserTheme_UsesWarmObsidianSurfaces()
    {
        string repoRoot = FindRepoRoot();
        string themePath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "BrowserTheme.xaml");
        string xaml = File.ReadAllText(themePath);

        Assert.Contains("Color x:Key=\"BgPrimary\">#0E1017</Color>", xaml, StringComparison.Ordinal);
        Assert.Contains("Color x:Key=\"BgSurface\">#1D2230</Color>", xaml, StringComparison.Ordinal);
        Assert.Contains("Color x:Key=\"BgSurfaceHover\">#262D3C</Color>", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void BrowserTheme_HasWarmTextColors()
    {
        string repoRoot = FindRepoRoot();
        string themePath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "BrowserTheme.xaml");
        string xaml = File.ReadAllText(themePath);

        Assert.Contains("Color x:Key=\"TextPrimary\">#F0F0F5</Color>", xaml, StringComparison.Ordinal);
        Assert.Contains("Color x:Key=\"TextSecondary\">#9CA3B8</Color>", xaml, StringComparison.Ordinal);
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
