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
    public DbSet<NetworkLogEntity> NetworkLogs { get; set; } = null!;
    public DbSet<RuleEntity> Rules { get; set; } = null!;

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

        modelBuilder.Entity<NetworkLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired();
            entity.Property(e => e.Method).HasDefaultValue("GET");
            entity.Property(e => e.ResourceType).HasDefaultValue("Unknown");
            entity.Property(e => e.WasBlocked).HasDefaultValue(false);

            // Indexes for performance
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.WasBlocked);
            entity.HasIndex(e => e.ResourceType);
        });

        modelBuilder.Entity<RuleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Site).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RulesJson).IsRequired();
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.Priority).HasDefaultValue(10);
            entity.Property(e => e.IsEnforced).HasDefaultValue(false);
            entity.Property(e => e.Source).HasDefaultValue("local").HasMaxLength(50);

            // Indexes for performance
            entity.HasIndex(e => e.Enabled);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.Source);
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
