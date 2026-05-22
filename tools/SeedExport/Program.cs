// One-off exporter: reads the active profile's SQLite database and dumps the
// marketplace packs + joined channels (with their rules) to JSON files under
// /seed at the repo root. Those JSONs ship as EmbeddedResource in BrowserApp.UI
// and seed a fresh install's local DB on first launch (FirstRunSeedService).
//
// Run from repo root:  dotnet run --project tools/SeedExport
// Optional first arg overrides the active profile GUID.
//
// Re-run whenever you change the demo data (add packs, edit channel rules) and
// commit the updated JSON files.

using System.Text.Json;
using BrowserApp.Data;
using BrowserApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
string browserRoot  = Path.Combine(localAppData, "BrowserApp");
string activeFile   = Path.Combine(browserRoot, "active_profile.txt");

string? profileGuid = args.Length > 0 ? args[0] : null;
if (string.IsNullOrEmpty(profileGuid) && File.Exists(activeFile))
{
    profileGuid = File.ReadAllText(activeFile).Trim();
}
if (string.IsNullOrEmpty(profileGuid))
{
    var profilesDir = Path.Combine(browserRoot, "Profiles");
    if (Directory.Exists(profilesDir))
    {
        profileGuid = Directory.GetDirectories(profilesDir)
            .Select(Path.GetFileName)
            .FirstOrDefault(n => Guid.TryParse(n, out _));
    }
}
if (string.IsNullOrEmpty(profileGuid))
{
    Console.Error.WriteLine("Could not locate an active profile under %LOCALAPPDATA%\\BrowserApp\\Profiles.");
    return 1;
}

string dbPath = Path.Combine(browserRoot, "Profiles", profileGuid, "browser.db");
if (!File.Exists(dbPath))
{
    Console.Error.WriteLine($"Database not found at: {dbPath}");
    return 1;
}

Console.WriteLine($"Reading from profile {profileGuid}");
Console.WriteLine($"  DB: {dbPath}");

var options = new DbContextOptionsBuilder<BrowserDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

using var ctx = new BrowserDbContext(options);

var marketplaceRules = await ctx.Rules
    .Where(r => r.Source == "marketplace" && r.MarketplaceId != null)
    .OrderBy(r => r.Name)
    .ToListAsync();

var memberships = await ctx.ChannelMemberships
    .Where(m => m.IsActive)
    .ToListAsync();

var channelRules = await ctx.Rules
    .Where(r => r.Source == "channel" && r.ChannelId != null)
    .OrderBy(r => r.Name)
    .ToListAsync();

// --- Build JSON shapes ---------------------------------------------------

var marketplaceSeed = marketplaceRules.Select(r => new MarketplaceSeed
{
    MarketplaceId = r.MarketplaceId!,
    Name          = r.Name,
    Description   = r.Description,
    Site          = r.Site,
    Priority      = r.Priority,
    RulesJson     = r.RulesJson
}).ToList();

var channelSeed = memberships.Select(m => new ChannelSeed
{
    ChannelId          = m.ChannelId,
    ChannelName        = m.ChannelName,
    ChannelDescription = m.ChannelDescription,
    RuleCount          = m.RuleCount,
    Rules = channelRules
        .Where(r => r.ChannelId == m.ChannelId)
        .Select(r => new ChannelRuleSeed
        {
            Name        = r.Name,
            Description = r.Description,
            Site        = r.Site,
            Priority    = r.Priority,
            RulesJson   = r.RulesJson,
            IsEnforced  = r.IsEnforced
        }).ToList()
}).ToList();

// --- Write out -----------------------------------------------------------

string seedDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "seed"));
Directory.CreateDirectory(seedDir);

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

string marketplacePath = Path.Combine(seedDir, "marketplace.json");
string channelsPath    = Path.Combine(seedDir, "channels.json");

await File.WriteAllTextAsync(marketplacePath, JsonSerializer.Serialize(marketplaceSeed, jsonOptions));
await File.WriteAllTextAsync(channelsPath,    JsonSerializer.Serialize(channelSeed,    jsonOptions));

Console.WriteLine($"  Wrote {marketplaceSeed.Count} marketplace pack(s)  -> {marketplacePath}");
Console.WriteLine($"  Wrote {channelSeed.Count} channel(s) ({channelSeed.Sum(c => c.Rules.Count)} rules) -> {channelsPath}");
Console.WriteLine("Done.");
return 0;

// --- Seed DTOs (shape that ships in the JSON files) ----------------------

public record MarketplaceSeed
{
    public string MarketplaceId { get; init; } = string.Empty;
    public string Name          { get; init; } = string.Empty;
    public string Description   { get; init; } = string.Empty;
    public string Site          { get; init; } = "*";
    public int    Priority      { get; init; } = 10;
    public string RulesJson     { get; init; } = "[]";
}

public record ChannelSeed
{
    public string ChannelId          { get; init; } = string.Empty;
    public string ChannelName        { get; init; } = string.Empty;
    public string ChannelDescription { get; init; } = string.Empty;
    public int    RuleCount          { get; init; }
    public List<ChannelRuleSeed> Rules { get; init; } = new();
}

public record ChannelRuleSeed
{
    public string Name        { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Site        { get; init; } = "*";
    public int    Priority    { get; init; } = 10;
    public string RulesJson   { get; init; } = "[]";
    public bool   IsEnforced  { get; init; }
}
