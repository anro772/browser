using System.IO;
using System.Text.Json;
using BrowserApp.Core.Models;

namespace BrowserApp.UI.Services;

/// <summary>
/// Service for managing user settings persistence.
/// Settings are stored in LocalAppData as JSON.
/// </summary>
public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrowserApp",
        "settings.json");

    private UserSettings _settings = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService()
    {
        LoadSettings();
    }

    /// <summary>
    /// Gets the current user settings.
    /// </summary>
    public UserSettings Settings => _settings;

    /// <summary>
    /// Gets or sets the current privacy mode.
    /// </summary>
    public PrivacyMode PrivacyMode
    {
        get => _settings.PrivacyMode;
        set
        {
            _settings.PrivacyMode = value;
            SaveSettings();
            PrivacyModeChanged?.Invoke(this, value);
        }
    }

    /// <summary>
    /// Gets or sets the server URL for marketplace/channels.
    /// </summary>
    public string ServerUrl
    {
        get => _settings.ServerUrl;
        set
        {
            _settings.ServerUrl = value;
            SaveSettings();
        }
    }

    /// <summary>
    /// Event raised when privacy mode changes.
    /// </summary>
    public event EventHandler<PrivacyMode>? PrivacyModeChanged;

    /// <summary>
    /// Loads settings from disk.
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _settings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            _settings = new UserSettings();
        }
    }

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
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
}
