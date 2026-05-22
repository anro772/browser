using System.IO;
using System.Reflection;
using System.Text.Json;
using BrowserApp.Data;
using BrowserApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserApp.UI.Services;

/// <summary>
/// Seeds the local SQLite with the curated marketplace packs and channel(s)
/// the first time the app starts against an empty DB. The seed JSON files are
/// embedded in this assembly under <c>BrowserApp.UI.Seed.*.json</c> and were
/// produced by <c>tools/SeedExport</c>.
///
/// The seed is intentionally narrow: marketplace + channel content only. It
/// does not touch profiles, settings, history, bookmarks, or local rules.
/// Idempotent: each surface short-circuits if data already exists.
/// </summary>
public class FirstRunSeedService
{
    private const string MarketplaceResource = "BrowserApp.UI.Seed.marketplace.json";
    private const string ChannelsResource    = "BrowserApp.UI.Seed.channels.json";

    // The username stamped on freshly-seeded memberships. Matches the initial
    // value of ChannelsViewModel._username so the membership "feels native"
    // before the user touches Settings. Membership queries don't filter by
    // username (see ChannelSyncService.GetJoinedChannelsAsync), so this is
    // purely a record-keeping value.
    private const string SeedUsername = "default_user";

    private readonly BrowserDbContext _db;

    public FirstRunSeedService(BrowserDbContext db)
    {
        _db = db;
    }

    public async Task SeedIfNeededAsync()
    {
        try
        {
            await SeedMarketplaceAsync();
            await SeedChannelsAsync();
        }
        catch (Exception ex)
        {
            // Never crash startup. An empty marketplace/channel view is a
            // recoverable state — a broken seed is not.
            ErrorLogger.LogError("FirstRunSeedService failed (non-fatal)", ex);
        }
    }

    private async Task SeedMarketplaceAsync()
    {
        bool any = await _db.Rules.AnyAsync(r => r.Source == "marketplace");
        if (any) return;

        var seed = LoadJson<List<MarketplaceSeed>>(MarketplaceResource);
        if (seed == null || seed.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var s in seed)
        {
            _db.Rules.Add(new RuleEntity
            {
                Id            = Guid.NewGuid().ToString(),
                Name          = s.Name,
                Description   = s.Description,
                Site          = s.Site,
                Priority      = s.Priority,
                RulesJson     = s.RulesJson,
                Source        = "marketplace",
                MarketplaceId = s.MarketplaceId,
                Enabled       = true,
                IsEnforced    = false,
                CreatedAt     = now,
                UpdatedAt     = now
            });
        }

        await _db.SaveChangesAsync();
        ErrorLogger.LogInfo($"FirstRunSeed: inserted {seed.Count} marketplace pack(s).");
    }

    private async Task SeedChannelsAsync()
    {
        bool any = await _db.ChannelMemberships.AnyAsync();
        if (any) return;

        var seed = LoadJson<List<ChannelSeed>>(ChannelsResource);
        if (seed == null || seed.Count == 0) return;

        var now = DateTime.UtcNow;
        int totalRules = 0;
        foreach (var c in seed)
        {
            _db.ChannelMemberships.Add(new ChannelMembershipEntity
            {
                Id                 = Guid.NewGuid().ToString(),
                ChannelId          = c.ChannelId,
                ChannelName        = c.ChannelName,
                ChannelDescription = c.ChannelDescription,
                Username           = SeedUsername,
                IsActive           = true,
                JoinedAt           = now,
                LastSyncedAt       = now,
                RuleCount          = c.RuleCount
            });

            foreach (var r in c.Rules)
            {
                _db.Rules.Add(new RuleEntity
                {
                    Id          = Guid.NewGuid().ToString(),
                    Name        = r.Name,
                    Description = r.Description,
                    Site        = r.Site,
                    Priority    = r.Priority,
                    RulesJson   = r.RulesJson,
                    Source      = "channel",
                    ChannelId   = c.ChannelId,
                    Enabled     = true,
                    IsEnforced  = r.IsEnforced,
                    CreatedAt   = now,
                    UpdatedAt   = now
                });
                totalRules++;
            }
        }

        await _db.SaveChangesAsync();
        ErrorLogger.LogInfo($"FirstRunSeed: inserted {seed.Count} channel(s) and {totalRules} channel rule(s).");
    }

    private static T? LoadJson<T>(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            ErrorLogger.LogError($"FirstRunSeed: embedded resource '{resourceName}' not found", new InvalidOperationException(resourceName));
            return default;
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
    }

    // Mirror of the seed DTOs in tools/SeedExport/Program.cs.
    private record MarketplaceSeed(
        string MarketplaceId,
        string Name,
        string Description,
        string Site,
        int    Priority,
        string RulesJson);

    private record ChannelSeed(
        string ChannelId,
        string ChannelName,
        string ChannelDescription,
        int    RuleCount,
        List<ChannelRuleSeed> Rules);

    private record ChannelRuleSeed(
        string Name,
        string Description,
        string Site,
        int    Priority,
        string RulesJson,
        bool   IsEnforced);
}
