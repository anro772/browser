using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Data.Entities;
using BrowserApp.UI.DTOs;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the Channels panel.
/// Handles channel browsing, joining, leaving, and syncing.
/// </summary>
public partial class ChannelsViewModel : ObservableObject
{
    private readonly ChannelApiClient _apiClient;
    private readonly ChannelSyncService _syncService;
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty]
    private ObservableCollection<ChannelItemViewModel> _availableChannels = new();

    [ObservableProperty]
    private ObservableCollection<JoinedChannelViewModel> _joinedChannels = new();

    [ObservableProperty]
    private ChannelItemViewModel? _selectedChannel;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _username = "default_user";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ChannelsViewModel(
        ChannelApiClient apiClient,
        ChannelSyncService syncService,
        IServiceScopeFactory scopeFactory)
    {
        _apiClient = apiClient;
        _syncService = syncService;
        _scopeFactory = scopeFactory;
    }

    [RelayCommand]
    private async Task LoadChannelsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading channels...";

        try
        {
            // Load available channels from server
            var response = await _apiClient.GetChannelsTypedAsync(1, 50);
            if (response != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableChannels.Clear();
                    foreach (var channel in response.Channels)
                    {
                        AvailableChannels.Add(new ChannelItemViewModel(channel));
                    }
                });
            }

            // Load joined channels from local database
            var joined = await _syncService.GetJoinedChannelsTypedAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                JoinedChannels.Clear();
                foreach (var membership in joined)
                {
                    JoinedChannels.Add(new JoinedChannelViewModel(membership));
                }
            });

            StatusMessage = $"Loaded {AvailableChannels.Count} channels, joined {JoinedChannels.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading channels: {ex.Message}";
            ErrorLogger.LogError("Failed to load channels", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task JoinChannelAsync(ChannelItemViewModel? channel)
    {
        if (channel == null) return;

        // Show password dialog
        var password = await ShowPasswordDialogAsync(channel.Name);
        if (string.IsNullOrEmpty(password)) return;

        IsLoading = true;
        StatusMessage = $"Joining '{channel.Name}'...";

        try
        {
            var success = await _syncService.JoinChannelAsync(
                channel.Id,
                channel.Name,
                channel.Description,
                Username,
                password);

            if (success)
            {
                StatusMessage = $"Joined '{channel.Name}' successfully!";
                await LoadChannelsAsync();
            }
            else
            {
                StatusMessage = "Failed to join channel. Check the password.";
                MessageBox.Show("Failed to join channel. Please check the password.",
                    "Join Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error joining channel: {ex.Message}";
            ErrorLogger.LogError($"Failed to join channel {channel.Id}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LeaveChannelAsync(JoinedChannelViewModel? channel)
    {
        if (channel == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to leave '{channel.ChannelName}'?\n\nAll rules from this channel will be removed.",
            "Leave Channel",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;
        StatusMessage = $"Leaving '{channel.ChannelName}'...";

        try
        {
            var success = await _syncService.LeaveChannelAsync(channel.ChannelId, Username);
            if (success)
            {
                StatusMessage = $"Left '{channel.ChannelName}' successfully!";
                await LoadChannelsAsync();
            }
            else
            {
                StatusMessage = "Failed to leave channel.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error leaving channel: {ex.Message}";
            ErrorLogger.LogError($"Failed to leave channel {channel.ChannelId}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        IsLoading = true;
        StatusMessage = "Syncing all channels...";

        try
        {
            await _syncService.SyncAllChannelsAsync(Username);
            await LoadChannelsAsync();
            StatusMessage = "Sync complete!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sync failed: {ex.Message}";
            ErrorLogger.LogError("Failed to sync channels", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateChannelAsync()
    {
        // Show create channel dialog
        var (name, description, password) = await ShowCreateChannelDialogAsync();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(password)) return;

        IsLoading = true;
        StatusMessage = $"Creating channel '{name}'...";

        try
        {
            var result = await _apiClient.CreateChannelTypedAsync(new CreateChannelRequest
            {
                Name = name,
                Description = description,
                OwnerUsername = Username,
                Password = password,
                IsPublic = true
            });

            if (result != null)
            {
                StatusMessage = $"Channel '{name}' created!";
                await LoadChannelsAsync();
            }
            else
            {
                StatusMessage = "Failed to create channel.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating channel: {ex.Message}";
            ErrorLogger.LogError($"Failed to create channel '{name}'", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Task<string> ShowPasswordDialogAsync(string channelName)
    {
        // Simple password input using MessageBox for now (skeleton UI)
        // In a real app, this would be a proper dialog
        var password = Microsoft.VisualBasic.Interaction.InputBox(
            $"Enter password for channel '{channelName}':",
            "Join Channel",
            "");
        return Task.FromResult(password);
    }

    private Task<(string name, string description, string password)> ShowCreateChannelDialogAsync()
    {
        // Simple input using MessageBox for now (skeleton UI)
        var name = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter channel name:",
            "Create Channel",
            "");
        if (string.IsNullOrEmpty(name)) return Task.FromResult<(string, string, string)>(("", "", ""));

        var description = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter channel description:",
            "Create Channel",
            "");

        var password = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter channel password:",
            "Create Channel",
            "");
        if (string.IsNullOrEmpty(password)) return Task.FromResult<(string, string, string)>(("", "", ""));

        return Task.FromResult((name, description, password));
    }
}

/// <summary>
/// ViewModel for a single channel in the available channels list.
/// </summary>
public class ChannelItemViewModel
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string OwnerUsername { get; }
    public int MemberCount { get; }
    public int RuleCount { get; }
    public DateTime CreatedAt { get; }

    public ChannelItemViewModel(ChannelResponse response)
    {
        Id = response.Id;
        Name = response.Name;
        Description = response.Description;
        OwnerUsername = response.OwnerUsername;
        MemberCount = response.MemberCount;
        RuleCount = response.RuleCount;
        CreatedAt = response.CreatedAt;
    }

    public string DisplayInfo => $"{MemberCount} members • {RuleCount} rules";
}

/// <summary>
/// ViewModel for a joined channel.
/// </summary>
public class JoinedChannelViewModel
{
    public string ChannelId { get; }
    public string ChannelName { get; }
    public string ChannelDescription { get; }
    public DateTime JoinedAt { get; }
    public DateTime LastSyncedAt { get; }
    public int RuleCount { get; }

    public JoinedChannelViewModel(ChannelMembershipEntity entity)
    {
        ChannelId = entity.ChannelId;
        ChannelName = entity.ChannelName;
        ChannelDescription = entity.ChannelDescription;
        JoinedAt = entity.JoinedAt;
        LastSyncedAt = entity.LastSyncedAt;
        RuleCount = entity.RuleCount;
    }

    public string LastSyncedDisplay => $"Last synced: {LastSyncedAt:g}";
}
