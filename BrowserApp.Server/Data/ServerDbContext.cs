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
    public DbSet<ChannelEntity> Channels { get; set; }
    public DbSet<ChannelRuleEntity> ChannelRules { get; set; }
    public DbSet<ChannelMemberEntity> ChannelMembers { get; set; }
    public DbSet<ChannelAuditLogEntity> ChannelAuditLogs { get; set; }

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

        // Channel entity configuration
        modelBuilder.Entity<ChannelEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.MemberCount)
                .HasDefaultValue(0);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.IsPublic)
                .HasDefaultValue(true);

            // Foreign key to User (owner)
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsPublic);
        });

        // ChannelRule entity configuration
        modelBuilder.Entity<ChannelRuleEntity>(entity =>
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

            entity.Property(e => e.RulesJson)
                .IsRequired()
                .HasColumnType("jsonb");

            entity.Property(e => e.IsEnforced)
                .HasDefaultValue(true);

            // Foreign key to Channel
            entity.HasOne(e => e.Channel)
                .WithMany(c => c.Rules)
                .HasForeignKey(e => e.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.ChannelId);
        });

        // ChannelMember entity configuration
        modelBuilder.Entity<ChannelMemberEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Foreign keys
            entity.HasOne(e => e.Channel)
                .WithMany(c => c.Members)
                .HasForeignKey(e => e.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint on ChannelId + UserId
            entity.HasIndex(e => new { e.ChannelId, e.UserId })
                .IsUnique();

            // Indexes
            entity.HasIndex(e => e.ChannelId);
            entity.HasIndex(e => e.UserId);
        });

        // ChannelAuditLog entity configuration
        modelBuilder.Entity<ChannelAuditLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");

            // Foreign keys
            entity.HasOne(e => e.Channel)
                .WithMany()
                .HasForeignKey(e => e.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for common queries
            entity.HasIndex(e => e.ChannelId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
