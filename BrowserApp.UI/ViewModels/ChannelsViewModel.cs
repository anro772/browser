using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.DTOs;
using BrowserApp.Core.Interfaces;
using BrowserApp.UI.Services;
using BrowserApp.UI.Views;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the Channels panel.
/// Handles channel browsing, joining, leaving, and syncing.
/// </summary>
public partial class ChannelsViewModel : ObservableObject
{
    private readonly IChannelApiClient _apiClient;
    private readonly IChannelSyncService _syncService;
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
        IChannelApiClient apiClient,
        IChannelSyncService syncService,
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
            var response = await _apiClient.GetChannelsAsync(1, 50);
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
            var joined = await _syncService.GetJoinedChannelsAsync();
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
        var password = ShowPasswordDialog(channel.Name);
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
        var (name, description, password) = ShowCreateChannelDialog();
        if (string.IsNullOrEmpty(name)) return;

        IsLoading = true;
        StatusMessage = $"Creating channel '{name}'...";

        try
        {
            var result = await _apiClient.CreateChannelAsync(new CreateChannelRequest
            {
                Name = name,
                Description = description,
                OwnerUsername = Username,
                Password = password,
                IsPublic = true
            });

            if (result != null)
            {
                // Save local membership (owner is auto-joined on server)
                await _syncService.SaveLocalMembershipAsync(
                    result.Id.ToString(),
                    result.Name,
                    result.Description,
                    Username);

                StatusMessage = $"Channel '{name}' created successfully!";
                MessageBox.Show(
                    $"Channel '{name}' has been created successfully!",
                    "Channel Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                await LoadChannelsAsync();
            }
            else
            {
                StatusMessage = "Failed to create channel.";
                MessageBox.Show(
                    "Failed to create channel. Please try again.",
                    "Creation Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating channel: {ex.Message}";
            ErrorLogger.LogError($"Failed to create channel '{name}'", ex);
            MessageBox.Show(
                $"Error creating channel: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string ShowPasswordDialog(string channelName)
    {
        var dialog = new PasswordDialog
        {
            DialogTitle = "Join Channel",
            PromptText = $"Enter password for channel '{channelName}':",
            Owner = Application.Current.MainWindow
        };

        var result = dialog.ShowDialog();
        return result == true ? dialog.Password : string.Empty;
    }

    private (string name, string description, string password) ShowCreateChannelDialog()
    {
        // For MVP, using simple InputBox dialogs for name and description
        var name = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter channel name:",
            "Create Channel",
            "");
        if (string.IsNullOrEmpty(name)) return ("", "", "");

        var description = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter channel description:",
            "Create Channel",
            "");

        // Use proper password dialog for the password
        var passwordDialog = new PasswordDialog
        {
            DialogTitle = "Create Channel",
            PromptText = "Enter channel password (minimum 4 characters):",
            Owner = Application.Current.MainWindow
        };

        var result = passwordDialog.ShowDialog();
        if (result != true) return ("", "", "");

        var password = passwordDialog.Password;

        // Validate password length
        if (string.IsNullOrEmpty(password) || password.Length < 4)
        {
            MessageBox.Show(
                "Password must be at least 4 characters long.",
                "Invalid Password",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return ("", "", "");
        }

        return (name, description, password);
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

    public JoinedChannelViewModel(ChannelMembershipDto dto)
    {
        ChannelId = dto.ChannelId;
        ChannelName = dto.ChannelName;
        ChannelDescription = dto.ChannelDescription;
        JoinedAt = dto.JoinedAt;
        LastSyncedAt = dto.LastSyncedAt;
        RuleCount = dto.RuleCount;
    }

    public string LastSyncedDisplay => $"Last synced: {LastSyncedAt:g}";
}
