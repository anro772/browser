using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BrowserApp.Core.Models;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for managing user settings persistence.
/// Settings are stored in LocalAppData as JSON.
/// Implements INotifyPropertyChanged so chrome bindings (e.g. the active-profile
/// pill's privacy-mode label) refresh when settings change.
/// </summary>
public class SettingsService : INotifyPropertyChanged
{
    private static string? _customPath;
    private readonly string _settingsPath;

    private UserSettings _settings = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Sets a custom settings file path (for profile support).
    /// Must be called before SettingsService is constructed.
    /// </summary>
    public static void SetSettingsPath(string path)
    {
        _customPath = path;
    }

    public SettingsService()
    {
        _settingsPath = _customPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrowserApp",
            "settings.json");
        LoadSettings();
    }

    public UserSettings Settings => _settings;

    public PrivacyMode PrivacyMode
    {
        get => _settings.PrivacyMode;
        set
        {
            if (_settings.PrivacyMode == value) return;
            _settings.PrivacyMode = value;
            SaveSettings();
            OnPropertyChanged();
            PrivacyModeChanged?.Invoke(this, value);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string ServerUrl
    {
        get => _settings.ServerUrl;
        set
        {
            _settings.ServerUrl = value;
            SaveSettings();
        }
    }

    public string SearchEngine
    {
        get => _settings.SearchEngine;
        set
        {
            _settings.SearchEngine = value;
            SaveSettings();
            SearchEngineChanged?.Invoke(this, value);
        }
    }

    public string CustomSearchEngineUrl
    {
        get => _settings.CustomSearchEngineUrl;
        set
        {
            _settings.CustomSearchEngineUrl = value;
            SaveSettings();
        }
    }

    public string HomePage
    {
        get => _settings.HomePage;
        set
        {
            _settings.HomePage = value;
            SaveSettings();
        }
    }

    public string DefaultDownloadPath
    {
        get => _settings.DefaultDownloadPath;
        set
        {
            _settings.DefaultDownloadPath = value;
            SaveSettings();
        }
    }

    public StartupBehavior StartupBehavior
    {
        get => _settings.StartupBehavior;
        set
        {
            _settings.StartupBehavior = value;
            SaveSettings();
        }
    }

    public bool HasMigratedToFilterListPrimary
    {
        get => _settings.HasMigratedToFilterListPrimary;
        set
        {
            _settings.HasMigratedToFilterListPrimary = value;
            SaveSettings();
        }
    }

    /// <summary>
    /// Last observed ABP toggle state. Read synchronously by MainViewModel on construction
    /// so the title-bar shield indicator is correct on the very first paint, instead of
    /// flickering when the fire-and-forget DB query resolves a few hundred ms later.
    /// Kept in lockstep with the DB record by ExtensionService.RaiseAdBlockerStateChanged.
    /// </summary>
    public bool LastKnownAdBlockerEnabled
    {
        get => _settings.LastKnownAdBlockerEnabled;
        set
        {
            if (_settings.LastKnownAdBlockerEnabled == value) return;
            _settings.LastKnownAdBlockerEnabled = value;
            SaveSettings();
        }
    }

    public bool ShowBookmarksBar
    {
        get => _settings.ShowBookmarksBar;
        set
        {
            if (_settings.ShowBookmarksBar == value) return;
            _settings.ShowBookmarksBar = value;
            SaveSettings();
            ShowBookmarksBarChanged?.Invoke(this, value);
        }
    }

    public string Username
    {
        get => _settings.Username;
        set
        {
            _settings.Username = value;
            SaveSettings();
            UsernameChanged?.Invoke(this, value);
        }
    }

    public string UserTag
    {
        get => _settings.UserTag;
        set
        {
            _settings.UserTag = value;
            SaveSettings();
        }
    }

    /// <summary>
    /// Display name with Discord-style tag, e.g. "username#1234".
    /// </summary>
    public string DisplayUsername => string.IsNullOrEmpty(Username)
        ? "user"
        : $"{Username}#{UserTag}";

    /// <summary>
    /// Just the username string for API calls.
    /// </summary>
    public string ApiUsername => string.IsNullOrEmpty(Username) ? "default_user" : Username;

    /// <summary>
    /// Initializes username and tag on first launch if not already set.
    /// Username falls back to a freshly-generated unique handle ("user_xxxxxx")
    /// instead of the profile name — without this every fresh install would end
    /// up with the same identity (e.g. "Default") and impersonate each other on
    /// channel/marketplace ownership checks. Existing installs that already have
    /// a username persisted keep it untouched.
    /// </summary>
    public void InitializeUsernameIfNeeded(string profileName)
    {
        if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.UserTag))
            return;

        if (string.IsNullOrEmpty(_settings.Username))
            _settings.Username = GenerateUniqueHandle();

        if (string.IsNullOrEmpty(_settings.UserTag))
            _settings.UserTag = Random.Shared.Next(1000, 10000).ToString();

        SaveSettings();
    }

    /// <summary>
    /// "user_" + 6 lowercase hex chars from a fresh GUID. Stable across launches
    /// once persisted to settings.json. Collision odds at this length are ~1 in
    /// 16M per install — fine for personal multi-machine + share-with-friends use.
    /// </summary>
    private static string GenerateUniqueHandle()
    {
        var slug = Guid.NewGuid().ToString("N").Substring(0, 6);
        return $"user_{slug}";
    }

    public event EventHandler<PrivacyMode>? PrivacyModeChanged;
    public event EventHandler<string>? SearchEngineChanged;
    public event EventHandler<string>? UsernameChanged;
    public event EventHandler<bool>? ShowBookmarksBarChanged;

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            _settings = new UserSettings();
        }
    }

    private void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}

/// <summary>
/// User settings model for JSON serialization.
/// </summary>
public class UserSettings
{
    public PrivacyMode PrivacyMode { get; set; } = PrivacyMode.Standard;
    public string ServerUrl { get; set; } = "http://localhost:5000";
    public string SearchEngine { get; set; } = "Google";
    public string CustomSearchEngineUrl { get; set; } = string.Empty;
    public string HomePage { get; set; } = string.Empty;
    public string DefaultDownloadPath { get; set; } = string.Empty;
    public StartupBehavior StartupBehavior { get; set; } = StartupBehavior.RestoreSession;
    public string Username { get; set; } = string.Empty;
    public string UserTag { get; set; } = string.Empty;
    public bool ShowBookmarksBar { get; set; } = false;

    /// <summary>
    /// One-shot migration marker: when switching FilterListService back to being the
    /// primary ad/tracker blocker (previously delegated to the ABP extension),
    /// we disable any currently-enabled ABP built-in so the user gets a clean
    /// FilterListService-only experience to evaluate. Once the migration runs,
    /// this flag stays true and the user's Settings toggle is honored thereafter.
    /// </summary>
    public bool HasMigratedToFilterListPrimary { get; set; } = false;

    /// <summary>
    /// Persisted snapshot of the built-in ad-blocker toggle. Lets the title-bar shield
    /// indicator render correctly on first paint without waiting for a DB round-trip.
    /// </summary>
    public bool LastKnownAdBlockerEnabled { get; set; } = false;
}

/// <summary>
/// Defines what the browser does on startup.
/// </summary>
public enum StartupBehavior
{
    RestoreSession,
    NewTab,
    HomePage
}
