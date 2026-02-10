using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Core.Models;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the profile selector.
/// Allows viewing, creating, switching, and deleting profiles.
/// </summary>
public partial class ProfileSelectorViewModel : ObservableObject
{
    private readonly ProfileService _profileService;

    [ObservableProperty]
    private string _newProfileName = string.Empty;

    [ObservableProperty]
    private string _selectedColor = "#0078D4";

    public ObservableCollection<BrowserProfile> Profiles { get; } = new();

    public BrowserProfile ActiveProfile => _profileService.ActiveProfile;

    public string[] AvailableColors { get; } = new[]
    {
        "#0078D4", // Blue
        "#107C10", // Green
        "#E74856", // Red
        "#FF8C00", // Orange
        "#881798", // Purple
        "#00B7C3", // Teal
        "#767676", // Gray
        "#FFB900"  // Yellow
    };

    public ProfileSelectorViewModel(ProfileService profileService)
    {
        _profileService = profileService;
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _profileService.Profiles)
        {
            Profiles.Add(profile);
        }
    }

    [RelayCommand]
    private void CreateProfile()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName)) return;

        _profileService.CreateProfile(NewProfileName.Trim(), SelectedColor);
        NewProfileName = string.Empty;
        LoadProfiles();
    }

    [RelayCommand]
    private void SwitchProfile(BrowserProfile? profile)
    {
        if (profile == null || profile.Id == ActiveProfile.Id) return;

        var result = MessageBox.Show(
            $"Switch to profile \"{profile.Name}\"?\n\nThe browser will restart to load the new profile.",
            "Switch Profile",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _profileService.SwitchProfile(profile.Id);
            RestartApplication();
        }
    }

    [RelayCommand]
    private void DeleteProfile(BrowserProfile? profile)
    {
        if (profile == null || profile.IsDefault) return;

        var result = MessageBox.Show(
            $"Delete profile \"{profile.Name}\"?\n\nAll bookmarks, history, and settings for this profile will be permanently deleted.",
            "Delete Profile",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _profileService.DeleteProfile(profile.Id);
            LoadProfiles();
        }
    }

    private static void RestartApplication()
    {
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            System.Diagnostics.Process.Start(exePath);
        }
        Application.Current.Shutdown();
    }
}
