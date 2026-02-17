using System.Text.RegularExpressions;

namespace BrowserApp.Tests.Styles;

public class BrowserThemeDataGridTests
{
    [Fact]
    public void ThemedDataGrid_UsesThemedHeaderRowAndCellStyles()
    {
        string repoRoot = FindRepoRoot();
        string themePath = Path.Combine(repoRoot, "BrowserApp.UI", "Styles", "BrowserTheme.xaml");
        string xaml = File.ReadAllText(themePath);

        // Ensure themed data grids don't fall back to default light headers/rows/cells.
        var regex = new Regex(
            "<Style\\s+x:Key=\"ThemedDataGrid\"[\\s\\S]*?<Setter\\s+Property=\"RowStyle\"\\s+Value=\"\\{DynamicResource\\s+ThemedDataGridRow\\}\"\\s*/>[\\s\\S]*?<Setter\\s+Property=\"ColumnHeaderStyle\"\\s+Value=\"\\{DynamicResource\\s+ThemedDataGridColumnHeader\\}\"\\s*/>[\\s\\S]*?<Setter\\s+Property=\"CellStyle\"\\s+Value=\"\\{DynamicResource\\s+ThemedDataGridCell\\}\"\\s*/>",
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
