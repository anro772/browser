using System.IO;
using System.Text.Json;
using BrowserApp.Core.Models;

namespace BrowserApp.UI.Services;

/// <summary>
/// Manages browser profiles with isolated data directories.
/// Each profile gets its own database, settings, and WebView2 user data folder.
/// Profile switching requires app restart (industry standard approach).
/// </summary>
public class ProfileService
{
    private static readonly string AppDataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrowserApp");

    private static readonly string ProfilesFilePath = Path.Combine(AppDataRoot, "profiles.json");
    private static readonly string ActiveProfileFilePath = Path.Combine(AppDataRoot, "active_profile.txt");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private List<BrowserProfile> _profiles = new();
    private BrowserProfile? _activeProfile;

    /// <summary>
    /// All configured profiles.
    /// </summary>
    public IReadOnlyList<BrowserProfile> Profiles => _profiles.AsReadOnly();

    /// <summary>
    /// The currently active profile.
    /// </summary>
    public BrowserProfile ActiveProfile => _activeProfile!;

    /// <summary>
    /// Initializes the profile service. Creates default profile on first run.
    /// Must be called before any other profile operations.
    /// </summary>
    public void Initialize()
    {
        Directory.CreateDirectory(AppDataRoot);

        LoadProfiles();

        // If profiles.json was missing-or-corrupt AND data folders still exist on disk,
        // register those folders as profiles before falling through to the first-run
        // mint below. Prevents the "fresh Default orphans the user's real data" bug
        // we hit when profiles.json got truncated mid-session.
        if (_profiles.Count == 0)
        {
            RecoverProfilesFromDisk();
        }

        // First run: create default profile and migrate existing data
        if (_profiles.Count == 0)
        {
            var defaultProfile = new BrowserProfile
            {
                Name = "Default",
                Color = "#0078D4",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            };
            _profiles.Add(defaultProfile);
            SaveProfiles();

            MigrateExistingDataToDefaultProfile(defaultProfile);
        }

        // Determine active profile. Be defensive: if profiles.json was hand-edited or a
        // profile got deleted while active_profile.txt still pointed to it, LoadActiveProfile()
        // returns null. If no profile in the list is flagged IsDefault (also a fixable corruption),
        // fall back to the first profile. If the list is somehow empty, mint a fresh default.
        _activeProfile = LoadActiveProfile()
                      ?? _profiles.FirstOrDefault(p => p.IsDefault)
                      ?? _profiles.FirstOrDefault();

        if (_activeProfile == null)
        {
            ErrorLogger.LogInfo("[ProfileService] No profiles found during init — minting fresh Default.");
            _activeProfile = new BrowserProfile
            {
                Name = "Default",
                Color = "#7C6AEF",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            };
            _profiles.Add(_activeProfile);
            SaveProfiles();
        }
        else if (!_profiles.Any(p => p.IsDefault))
        {
            // Promote the active profile to Default so future launches don't trip the
            // same recovery branch.
            _activeProfile.IsDefault = true;
            SaveProfiles();
            ErrorLogger.LogInfo($"[ProfileService] No default profile found — promoted '{_activeProfile.Name}' to default.");
        }

        // Persist the active-profile pointer so a stale guid in active_profile.txt
        // gets corrected on this launch (otherwise the recovery would repeat every start).
        SaveActiveProfile(_activeProfile);

        // Ensure profile directory exists
        Directory.CreateDirectory(GetProfileDataPath(_activeProfile));
        Directory.CreateDirectory(GetProfileUserDataPath(_activeProfile));
    }

    /// <summary>
    /// Gets the database path for the active profile.
    /// </summary>
    public string GetDatabasePath()
    {
        return Path.Combine(GetProfileDataPath(ActiveProfile), "browser.db");
    }

    /// <summary>
    /// Gets the settings file path for the active profile.
    /// </summary>
    public string GetSettingsPath()
    {
        return Path.Combine(GetProfileDataPath(ActiveProfile), "settings.json");
    }

    /// <summary>
    /// Gets the WebView2 user data folder for the active profile.
    /// </summary>
    public string GetUserDataPath()
    {
        return GetProfileUserDataPath(ActiveProfile);
    }

    /// <summary>
    /// Creates a new profile.
    /// </summary>
    public BrowserProfile CreateProfile(string name, string color = "#0078D4")
    {
        var profile = new BrowserProfile
        {
            Name = name,
            Color = color,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _profiles.Add(profile);
        SaveProfiles();

        // Create profile directory structure
        Directory.CreateDirectory(GetProfileDataPath(profile));
        Directory.CreateDirectory(GetProfileUserDataPath(profile));

        return profile;
    }

    /// <summary>
    /// Updates the color of an existing profile.
    /// </summary>
    public bool UpdateProfileColor(Guid profileId, string color)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null) return false;

        profile.Color = color;
        SaveProfiles();
        return true;
    }

    /// <summary>
    /// Renames an existing profile. Returns false if name is empty or profile not found.
    /// </summary>
    public bool UpdateProfileName(Guid profileId, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null) return false;

        profile.Name = name.Trim();
        SaveProfiles();
        return true;
    }

    /// <summary>
    /// Deletes a profile and its data. Cannot delete the default profile.
    /// </summary>
    public bool DeleteProfile(Guid profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null || profile.IsDefault) return false;

        // Don't delete the active profile
        if (_activeProfile?.Id == profileId) return false;

        _profiles.Remove(profile);
        SaveProfiles();

        // Delete profile data directory
        var profilePath = GetProfileDataPath(profile);
        if (Directory.Exists(profilePath))
        {
            try
            {
                Directory.Delete(profilePath, recursive: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileService] Failed to delete profile data: {ex.Message}");
            }
        }

        return true;
    }

    /// <summary>
    /// Switches to a different profile. Returns true if app should restart.
    /// </summary>
    public bool SwitchProfile(Guid profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null) return false;

        SaveActiveProfile(profile);
        return true; // Caller should restart the app
    }

    private string GetProfileDataPath(BrowserProfile profile)
    {
        return Path.Combine(AppDataRoot, "Profiles", profile.Id.ToString());
    }

    private string GetProfileUserDataPath(BrowserProfile profile)
    {
        return Path.Combine(GetProfileDataPath(profile), "UserData");
    }

    private void LoadProfiles()
    {
        // True first run — file missing is expected, stay silent.
        if (!File.Exists(ProfilesFilePath))
        {
            _profiles = new();
            return;
        }

        try
        {
            var json = File.ReadAllText(ProfilesFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                ErrorLogger.LogError(
                    "[ProfileService] profiles.json is empty — folder-based recovery will run",
                    new Exception("Empty profiles.json"));
                BackupCorruptProfilesFile();
                _profiles = new();
                return;
            }

            var parsed = JsonSerializer.Deserialize<List<BrowserProfile>>(json);
            if (parsed == null)
            {
                ErrorLogger.LogError(
                    "[ProfileService] profiles.json deserialised to null — folder-based recovery will run",
                    new Exception("Null deserialisation result"));
                BackupCorruptProfilesFile();
                _profiles = new();
                return;
            }

            _profiles = parsed;
        }
        catch (Exception ex)
        {
            // Surface the failure loudly. Earlier versions only wrote to Debug.WriteLine
            // which silently swallowed corruption events — making post-mortem impossible.
            ErrorLogger.LogError(
                "[ProfileService] profiles.json corrupt — folder-based recovery will run",
                ex);
            BackupCorruptProfilesFile();
            _profiles = new();
        }
    }

    /// <summary>
    /// Copies the (currently corrupt) <c>profiles.json</c> to
    /// <c>profiles.json.corrupt_&lt;yyyyMMdd_HHmmss&gt;</c> so the original is preserved
    /// for forensic / manual recovery before we overwrite it with a freshly-rebuilt
    /// version. Failure to back up is non-fatal — we still proceed to recovery.
    /// </summary>
    private void BackupCorruptProfilesFile()
    {
        try
        {
            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backup = ProfilesFilePath + $".corrupt_{ts}";
            File.Copy(ProfilesFilePath, backup, overwrite: true);
            ErrorLogger.LogInfo($"[ProfileService] Backed up corrupt profiles.json to {Path.GetFileName(backup)}");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("[ProfileService] Failed to back up corrupt profiles.json", ex);
        }
    }

    private void SaveProfiles()
    {
        try
        {
            var json = JsonSerializer.Serialize(_profiles, JsonOptions);
            File.WriteAllText(ProfilesFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Failed to save profiles: {ex.Message}");
        }
    }

    /// <summary>
    /// Scans the on-disk <c>Profiles/</c> folder for any data directories that look like
    /// real profiles (folder name is a valid GUID AND it contains a <c>browser.db</c>) and
    /// registers them as <see cref="BrowserProfile"/> entries. Called by
    /// <see cref="Initialize"/> only when <see cref="LoadProfiles"/> ended with an empty
    /// list, so it never duplicates registrations of profiles the JSON already described.
    ///
    /// The folder data IS the source of truth — the JSON is just an index over it.
    /// When the index is lost (corruption, hand-edit, accidental delete), this scan
    /// rebuilds it so the user's data isn't orphaned and a fresh "Default" mint isn't
    /// pasted over the top of perfectly good folders.
    /// </summary>
    private void RecoverProfilesFromDisk()
    {
        var profilesRoot = Path.Combine(AppDataRoot, "Profiles");
        if (!Directory.Exists(profilesRoot)) return;

        var recovered = new List<(BrowserProfile profile, DateTime dbMtime)>();
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(profilesRoot))
            {
                var folderName = Path.GetFileName(dir);
                if (!Guid.TryParse(folderName, out var folderId)) continue;

                var dbPath = Path.Combine(dir, "browser.db");
                if (!File.Exists(dbPath)) continue;

                // Defensive: skip if already registered by an earlier code path.
                if (_profiles.Any(p => p.Id == folderId)) continue;

                var profile = new BrowserProfile
                {
                    Id = folderId,
                    Name = $"Recovered ({folderName[..8]})",
                    Color = "#7C6AEF",
                    IsDefault = false,
                    CreatedAt = Directory.GetCreationTimeUtc(dir),
                };
                recovered.Add((profile, File.GetLastWriteTimeUtc(dbPath)));
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("[ProfileService] Profile folder recovery scan failed", ex);
            return;
        }

        if (recovered.Count == 0) return;

        // Add in most-recent-DB-write-first order. If both pointer files are gone, the
        // existing _profiles.FirstOrDefault() fallback in Initialize then lands on the
        // most-recently-used profile — which is almost always what the user wants.
        foreach (var (profile, _) in recovered.OrderByDescending(r => r.dbMtime))
        {
            _profiles.Add(profile);
        }

        ErrorLogger.LogInfo($"[ProfileService] Recovered {recovered.Count} profile(s) from disk folders");
        SaveProfiles();
    }

    private BrowserProfile? LoadActiveProfile()
    {
        try
        {
            if (File.Exists(ActiveProfileFilePath))
            {
                var idStr = File.ReadAllText(ActiveProfileFilePath).Trim();
                if (Guid.TryParse(idStr, out var id))
                {
                    return _profiles.FirstOrDefault(p => p.Id == id);
                }
            }
        }
        catch { }

        return null;
    }

    private void SaveActiveProfile(BrowserProfile profile)
    {
        try
        {
            File.WriteAllText(ActiveProfileFilePath, profile.Id.ToString());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Failed to save active profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Moves existing root-level data into the default profile directory on first run.
    /// </summary>
    private void MigrateExistingDataToDefaultProfile(BrowserProfile defaultProfile)
    {
        var profilePath = GetProfileDataPath(defaultProfile);
        Directory.CreateDirectory(profilePath);

        // Migrate database
        var existingDb = Path.Combine(AppDataRoot, "browser.db");
        var newDb = Path.Combine(profilePath, "browser.db");
        if (File.Exists(existingDb) && !File.Exists(newDb))
        {
            try
            {
                File.Move(existingDb, newDb);
                ErrorLogger.LogInfo($"[ProfileService] Migrated database to default profile");
            }
            catch (Exception ex)
            {
                // Copy instead if move fails
                try { File.Copy(existingDb, newDb); } catch { }
                ErrorLogger.LogError("[ProfileService] Database migration error", ex);
            }
        }

        // Migrate settings
        var existingSettings = Path.Combine(AppDataRoot, "settings.json");
        var newSettings = Path.Combine(profilePath, "settings.json");
        if (File.Exists(existingSettings) && !File.Exists(newSettings))
        {
            try
            {
                File.Move(existingSettings, newSettings);
            }
            catch { try { File.Copy(existingSettings, newSettings); } catch { } }
        }

        // Migrate WebView2 user data
        var existingUserData = Path.Combine(AppDataRoot, "UserData");
        var newUserData = GetProfileUserDataPath(defaultProfile);
        if (Directory.Exists(existingUserData) && !Directory.Exists(newUserData))
        {
            try
            {
                Directory.Move(existingUserData, newUserData);
                ErrorLogger.LogInfo("[ProfileService] Migrated UserData to default profile");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("[ProfileService] UserData migration error", ex);
                // Create fresh directory
                Directory.CreateDirectory(newUserData);
            }
        }
        else
        {
            Directory.CreateDirectory(newUserData);
        }
    }
}
