using System.IO;
using System.Text.Json;
using BrowserApp.Data.Entities;

namespace BrowserApp.UI.Services;

/// <summary>
/// Per-profile JSON snapshot of the user's bookmarks. SQLite remains the source of truth;
/// this service exists purely so the bookmarks bar can render on the very first WPF paint
/// without waiting for EF Core's first-query cold tax (~100–200 ms) to clear.
///
/// Mirrors Chrome's <c>Bookmarks</c> file pattern: tiny, read-mostly, frequently displayed
/// data lives in a plain JSON file alongside the per-profile database.
///
/// Lifecycle:
///   • <see cref="Load"/> is called synchronously in <c>BookmarkViewModel</c>'s constructor.
///   • <see cref="SaveAsync"/> is called fire-and-forget after every DB mutation
///     (<c>ToggleBookmarkAsync</c>, <c>RemoveBookmarkAsync</c>) so the JSON tracks the DB.
///   • A post-show reconcile pass in <c>App.xaml.cs</c> reads the DB once and rewrites the
///     JSON, so external edits to the SQLite file eventually flow back into the snapshot.
/// </summary>
public class BookmarkSnapshotService
{
    private readonly ProfileService _profileService;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public BookmarkSnapshotService(ProfileService profileService)
    {
        _profileService = profileService;
    }

    private string SnapshotPath
    {
        get
        {
            var dir = Path.GetDirectoryName(_profileService.GetDatabasePath())
                      ?? throw new InvalidOperationException("Profile data directory could not be resolved");
            return Path.Combine(dir, "bookmarks.json");
        }
    }

    /// <summary>
    /// Synchronously reads the snapshot. Returns an empty list when the file is missing or
    /// can't be parsed — the post-show reconcile will repopulate it from the DB shortly.
    /// Never throws; corruption is logged and treated as "no snapshot yet".
    /// </summary>
    public IReadOnlyList<BookmarkEntity> Load()
    {
        var path = SnapshotPath;
        if (!File.Exists(path)) return Array.Empty<BookmarkEntity>();

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<BookmarkEntity>();

            var items = JsonSerializer.Deserialize<List<BookmarkEntity>>(json);
            return items ?? (IReadOnlyList<BookmarkEntity>)Array.Empty<BookmarkEntity>();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"[BookmarkSnapshot] Corrupt snapshot at {path} — treating as empty until reconcile", ex);
            return Array.Empty<BookmarkEntity>();
        }
    }

    /// <summary>
    /// Atomically writes the snapshot. Crash mid-write can't leave a half-written file
    /// because we serialize to a sibling <c>.tmp</c> and then move it over the real path.
    /// </summary>
    public async Task SaveAsync(IEnumerable<BookmarkEntity> items)
    {
        try
        {
            var path = SnapshotPath;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tmpPath = path + ".tmp";
            var json = JsonSerializer.Serialize(items, JsonOptions);
            await File.WriteAllTextAsync(tmpPath, json);
            File.Move(tmpPath, path, overwrite: true);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("[BookmarkSnapshot] Failed to save snapshot (non-fatal)", ex);
        }
    }
}
