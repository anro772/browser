using Microsoft.EntityFrameworkCore;
using BrowserApp.Data.Entities;

namespace BrowserApp.Data;

/// <summary>
/// Entity Framework Core database context for the browser application.
/// Uses SQLite for local data storage.
/// </summary>
public class BrowserDbContext : DbContext
{
    public DbSet<BrowsingHistoryEntity> BrowsingHistory { get; set; } = null!;
    public DbSet<SettingsEntity> Settings { get; set; } = null!;

    public BrowserDbContext()
    {
    }

    public BrowserDbContext(DbContextOptions<BrowserDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string dbPath = GetDatabasePath();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BrowsingHistoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired();
            entity.HasIndex(e => e.VisitedAt);
        });

        modelBuilder.Entity<SettingsEntity>(entity =>
        {
            entity.HasKey(e => e.Key);
        });
    }

    /// <summary>
    /// Gets the path to the SQLite database file.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    public static string GetDatabasePath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string browserAppPath = Path.Combine(appDataPath, "BrowserApp");

        if (!Directory.Exists(browserAppPath))
        {
            Directory.CreateDirectory(browserAppPath);
        }

        return Path.Combine(browserAppPath, "browser.db");
    }
}
