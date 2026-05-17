using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Core.Models;
using BrowserApp.Core.Interfaces;
using BrowserApp.UI.Services;
using BrowserApp.UI.Views;

namespace BrowserApp.UI.ViewModels;

public partial class ProfileSelectorViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly IChannelSyncService _syncService;

    [ObservableProperty]
    private string _activeProfileChannelSummary = string.Empty;

    [ObservableProperty]
    private int _lastSavedCounter;

    public ObservableCollection<BrowserProfile> Profiles { get; } = new();

    public BrowserProfile ActiveProfile => _profileService.ActiveProfile;

    public ProfileSelectorViewModel(ProfileService profileService, IChannelSyncService syncService)
    {
        _profileService = profileService;
        _syncService = syncService;
        LoadProfiles();
        _ = LoadChannelStatsAsync();
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        var activeId = _profileService.ActiveProfile.Id;
        foreach (var profile in _profileService.Profiles)
        {
            profile.IsActive = profile.Id == activeId;
            Profiles.Add(profile);
        }
    }

    [RelayCommand]
    private void CreateProfile()
    {
        var dialog = new CreateProfileDialog
        {
            DialogTitle = "Create Profile",
            DialogSubtitle = "Create a new browser profile with its own data.",
            ShowNameField = true,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ProfileName))
        {
            _profileService.CreateProfile(dialog.ProfileName, dialog.SelectedColor);
            LoadProfiles();
        }
    }

    [RelayCommand]
    private void ChangeProfileColor(BrowserProfile? profile)
    {
        if (profile == null) return;

        var dialog = new CreateProfileDialog
        {
            DialogTitle = "Change Color",
            DialogSubtitle = $"Pick a new color for \"{profile.Name}\".",
            ShowNameField = false,
            SelectedColor = profile.Color,
            Owner = Application.Current.MainWindow
        };
        dialog.ConfirmButton.Content = "Save";

        if (dialog.ShowDialog() == true)
        {
            _profileService.UpdateProfileColor(profile.Id, dialog.SelectedColor);
            LoadProfiles();
            OnPropertyChanged(nameof(ActiveProfile));
            LastSavedCounter++;
        }
    }

    [RelayCommand]
    private void RenameProfile(BrowserProfile? profile)
    {
        if (profile == null) return;

        var dialog = new CreateProfileDialog
        {
            DialogTitle = "Rename Profile",
            DialogSubtitle = $"Choose a new name for \"{profile.Name}\".",
            ShowNameField = true,
            ShowColorPicker = false,
            InitialName = profile.Name,
            Owner = Application.Current.MainWindow
        };
        dialog.ConfirmButton.Content = "Save";

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ProfileName))
        {
            _profileService.UpdateProfileName(profile.Id, dialog.ProfileName);
            LoadProfiles();
            OnPropertyChanged(nameof(ActiveProfile));
            LastSavedCounter++;
        }
    }

    [RelayCommand]
    private void SwitchProfile(BrowserProfile? profile)
    {
        if (profile == null || profile.Id == ActiveProfile.Id) return;

        if (!ConfirmDialog.Show(Application.Current.MainWindow,
                "Switch profile",
                $"Switch to profile \"{profile.Name}\"?\n\nThe browser will restart to load the new profile.",
                confirmText: "Switch & restart"))
        {
            return;
        }

        _profileService.SwitchProfile(profile.Id);
        RestartApplication();
    }

    [RelayCommand]
    private void DeleteProfile(BrowserProfile? profile)
    {
        if (profile == null || profile.IsDefault) return;

        if (!ConfirmDialog.Show(Application.Current.MainWindow,
                "Delete profile",
                $"Delete profile \"{profile.Name}\"?\n\nAll bookmarks, history, and settings for this profile will be permanently deleted.",
                destructive: true,
                confirmText: "Delete"))
        {
            return;
        }

        _profileService.DeleteProfile(profile.Id);
        LoadProfiles();
    }

    private async Task LoadChannelStatsAsync()
    {
        try
        {
            var channels = (await _syncService.GetJoinedChannelsAsync()).ToList();
            var ruleCount = channels.Sum(c => c.RuleCount);
            ActiveProfileChannelSummary = channels.Count > 0
                ? $"{channels.Count} channels, {ruleCount} channel rules"
                : "No channels joined";
        }
        catch
        {
            ActiveProfileChannelSummary = "";
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
