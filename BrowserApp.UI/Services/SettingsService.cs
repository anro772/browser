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
            _settings.PrivacyMode = value;
            SaveSettings();
            PrivacyModeChanged?.Invoke(this, value);
        }
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

    public event EventHandler<PrivacyMode>? PrivacyModeChanged;
    public event EventHandler<string>? SearchEngineChanged;

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
}
