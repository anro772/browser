using Microsoft.EntityFrameworkCore;
using BrowserApp.Server.Data.Entities;

namespace BrowserApp.Server.Data;

/// <summary>
/// Entity Framework Core context for the marketplace server.
/// Uses PostgreSQL for data storage.
/// </summary>
public class ServerDbContext : DbContext
{
    public ServerDbContext(DbContextOptions<ServerDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserEntity> Users { get; set; }
    public DbSet<MarketplaceRuleEntity> MarketplaceRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User entity configuration
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);
            entity.HasIndex(e => e.Username)
                .IsUnique();
        });

        // MarketplaceRule entity configuration
        modelBuilder.Entity<MarketplaceRuleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Site)
                .IsRequired()
                .HasMaxLength(500);

            // Store as JSONB for efficient JSON queries in PostgreSQL
            entity.Property(e => e.RulesJson)
                .IsRequired()
                .HasColumnType("jsonb");

            // Store as text array in PostgreSQL
            entity.Property(e => e.Tags)
                .HasColumnType("text[]");

            entity.Property(e => e.DownloadCount)
                .HasDefaultValue(0);

            // Foreign key to User
            entity.HasOne(e => e.Author)
                .WithMany(u => u.Rules)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for common queries
            entity.HasIndex(e => e.DownloadCount);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.AuthorId);
        });
    }
}
