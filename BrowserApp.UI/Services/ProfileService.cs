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

        // Determine active profile
        _activeProfile = LoadActiveProfile() ?? _profiles.First(p => p.IsDefault);

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
        try
        {
            if (File.Exists(ProfilesFilePath))
            {
                var json = File.ReadAllText(ProfilesFilePath);
                _profiles = JsonSerializer.Deserialize<List<BrowserProfile>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileService] Failed to load profiles: {ex.Message}");
            _profiles = new();
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
