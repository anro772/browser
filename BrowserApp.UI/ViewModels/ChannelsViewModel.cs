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
    private readonly IRuleEngine _ruleEngine;
    private readonly SettingsService _settingsService;

    private List<ChannelItemViewModel> _allAvailableChannels = new();
    private List<UnifiedChannelViewModel> _allChannels = new();

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ChannelItemViewModel> _availableChannels = new();

    [ObservableProperty]
    private ObservableCollection<JoinedChannelViewModel> _joinedChannels = new();

    [ObservableProperty]
    private ObservableCollection<UnifiedChannelViewModel> _channels = new();

    [ObservableProperty]
    private ChannelItemViewModel? _selectedChannel;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _username = "default_user";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _showJoinedOnly;

    [ObservableProperty]
    private bool _isCreatePanelVisible;

    [ObservableProperty]
    private int _totalChannels;

    public ChannelsViewModel(
        IChannelApiClient apiClient,
        IChannelSyncService syncService,
        IServiceScopeFactory scopeFactory,
        IRuleEngine ruleEngine,
        SettingsService settingsService)
    {
        _apiClient = apiClient;
        _syncService = syncService;
        _scopeFactory = scopeFactory;
        _ruleEngine = ruleEngine;
        _settingsService = settingsService;
        Username = _settingsService.ApiUsername;
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

            // Load joined channels from local database
            var joined = await _syncService.GetJoinedChannelsAsync();
            var joinedList = joined.ToList();

            UiThread.Invoke(() =>
            {
                // Backward compat: populate old collections
                AvailableChannels.Clear();
                JoinedChannels.Clear();

                if (response != null)
                {
                    foreach (var channel in response.Channels)
                    {
                        AvailableChannels.Add(new ChannelItemViewModel(channel));
                    }
                    _allAvailableChannels = AvailableChannels.ToList();
                }

                foreach (var membership in joinedList)
                {
                    JoinedChannels.Add(new JoinedChannelViewModel(membership));
                }

                // Build unified list
                _allChannels.Clear();
                if (response != null)
                {
                    foreach (var channel in response.Channels)
                    {
                        var membership = joinedList.FirstOrDefault(m =>
                            m.ChannelId == channel.Id.ToString());
                        var vm = new UnifiedChannelViewModel(channel, membership);
                        vm.IsOwner = string.Equals(channel.OwnerUsername, Username, StringComparison.OrdinalIgnoreCase);
                        _allChannels.Add(vm);
                    }
                }

                // Sort: joined first (by name), then unjoined (by member count desc)
                _allChannels = _allChannels
                    .OrderByDescending(c => c.IsJoined)
                    .ThenBy(c => c.IsJoined ? c.Name : "")
                    .ThenByDescending(c => c.IsJoined ? 0 : c.MemberCount)
                    .ToList();

                TotalChannels = _allChannels.Count;
                FilterChannels();
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
    private async Task JoinChannelAsync(UnifiedChannelViewModel? channel)
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
                ConfirmDialog.Show(
                    Application.Current.MainWindow,
                    "Join failed",
                    "Failed to join channel. Please check the password.",
                    showCancel: false);
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
    private async Task LeaveChannelAsync(UnifiedChannelViewModel? channel)
    {
        if (channel == null) return;

        var confirmed = ConfirmDialog.Show(
            Application.Current.MainWindow,
            "Leave channel",
            $"Are you sure you want to leave '{channel.Name}'?\n\nAll rules from this channel will be removed.",
            destructive: true,
            confirmText: "Leave");

        if (!confirmed) return;

        IsLoading = true;
        StatusMessage = $"Leaving '{channel.Name}'...";

        try
        {
            var success = await _syncService.LeaveChannelAsync(channel.Id.ToString(), Username);
            if (success)
            {
                StatusMessage = $"Left '{channel.Name}' successfully!";
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
            ErrorLogger.LogError($"Failed to leave channel {channel.Id}", ex);
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
    private async Task ToggleExpandAsync(UnifiedChannelViewModel? channel)
    {
        if (channel == null) return;

        channel.IsExpanded = !channel.IsExpanded;

        if (channel.IsExpanded && channel.IsJoined && channel.RulePreview.Count == 0)
        {
            await LoadChannelRulesAsync(channel);
        }
    }

    /// <summary>
    /// Loads the channel's rule list into <see cref="UnifiedChannelViewModel.RulePreview"/>.
    /// Called from <see cref="ToggleExpandAsync"/> and from <see cref="ChannelDetailDialog"/>.
    /// </summary>
    public async Task LoadChannelRulesAsync(UnifiedChannelViewModel channel)
    {
        channel.IsLoadingPreview = true;
        try
        {
            var rulesResponse = await _apiClient.GetChannelRulesAsync(channel.Id, Username);
            if (rulesResponse != null)
            {
                UiThread.Invoke(() =>
                {
                    channel.RulePreview.Clear();
                    foreach (var rule in rulesResponse.Rules)
                    {
                        channel.RulePreview.Add(new RulePreviewItem(rule.Id, rule.ChannelId, rule.Name, rule.Site, rule.IsEnforced));
                    }
                });
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"Failed to load rule preview for channel {channel.Id}", ex);
        }
        finally
        {
            channel.IsLoadingPreview = false;
        }
    }

    [RelayCommand]
    private async Task SyncChannelAsync(UnifiedChannelViewModel? channel)
    {
        if (channel == null) return;

        IsLoading = true;
        StatusMessage = $"Syncing '{channel.Name}'...";

        try
        {
            await _syncService.SyncChannelRulesAsync(channel.Id.ToString(), Username);
            await LoadChannelsAsync();
            StatusMessage = $"Synced '{channel.Name}' successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error syncing channel: {ex.Message}";
            ErrorLogger.LogError($"Failed to sync channel {channel.Id}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ShowCreateDialogAsync()
    {
        var dialog = new Views.CreateChannelDialog
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        StatusMessage = $"Creating channel '{dialog.ChannelName}'...";

        try
        {
            var result = await _apiClient.CreateChannelAsync(new CreateChannelRequest
            {
                Name = dialog.ChannelName,
                Description = dialog.ChannelDescription,
                OwnerUsername = Username,
                Password = dialog.ChannelPassword,
                IsPublic = true
            });

            if (result != null)
            {
                await _syncService.SaveLocalMembershipAsync(
                    result.Id.ToString(),
                    result.Name,
                    result.Description,
                    Username);

                StatusMessage = $"Channel '{dialog.ChannelName}' created successfully!";
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
            ErrorLogger.LogError($"Failed to create channel '{dialog.ChannelName}'", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddRuleToChannelAsync(UnifiedChannelViewModel? channel)
    {
        if (channel == null) return;

        var viewModel = new RuleEditorViewModel(_scopeFactory, _ruleEngine)
        {
            ChannelId = channel.Id,
            ChannelApiClient = _apiClient,
            ChannelUsername = Username
        };

        var dialog = new RuleEditorDialog(viewModel)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            // Refresh rule preview
            channel.RulePreview.Clear();
            channel.IsExpanded = false;
            await ToggleExpandAsync(channel);
            StatusMessage = $"Rule added to '{channel.Name}' successfully!";
        }
    }

    [RelayCommand]
    private async Task DeleteChannelRuleAsync(RulePreviewItem? rule)
    {
        if (rule == null) return;

        var confirmed = ConfirmDialog.Show(
            Application.Current.MainWindow,
            "Remove rule",
            $"Remove rule '{rule.Name}' from this channel?",
            destructive: true,
            confirmText: "Remove");

        if (!confirmed) return;

        IsLoading = true;
        try
        {
            var success = await _apiClient.DeleteChannelRuleAsync(rule.ChannelId, rule.Id, Username);
            if (success)
            {
                // Remove from the parent channel's preview
                var channel = _allChannels.FirstOrDefault(c => c.Id == rule.ChannelId);
                if (channel != null)
                {
                    UiThread.Invoke(() => channel.RulePreview.Remove(rule));
                }
                StatusMessage = "Rule removed from channel.";
            }
            else
            {
                StatusMessage = "Failed to remove rule. You may not be the owner.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing rule: {ex.Message}";
            ErrorLogger.LogError($"Failed to delete channel rule {rule.Id}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnShowJoinedOnlyChanged(bool value) => FilterChannels();

    partial void OnSearchFilterChanged(string value) => FilterChannels();

    /// <summary>
    /// Opens the channel detail modal for inspecting rules + owner controls.
    /// </summary>
    [RelayCommand]
    private async Task ShowChannelDetailsAsync(UnifiedChannelViewModel? channel)
    {
        if (channel == null) return;

        var dialog = new ChannelDetailDialog(channel, this)
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();

        // Detail dialog may have mutated rules — refresh card view.
        if (channel.IsJoined)
        {
            await LoadChannelRulesAsync(channel);
        }
    }

    private void FilterChannels()
    {
        UiThread.Invoke(() =>
        {
            Channels.Clear();
            var filtered = _allChannels.AsEnumerable();

            if (ShowJoinedOnly)
                filtered = filtered.Where(c => c.IsJoined);

            if (!string.IsNullOrWhiteSpace(SearchFilter))
                filtered = filtered.Where(c =>
                    c.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));

            foreach (var c in filtered) Channels.Add(c);
        });

        // Also filter old collection for backward compat
        FilterAvailableChannels();
    }

    private void FilterAvailableChannels()
    {
        UiThread.Invoke(() =>
        {
            AvailableChannels.Clear();
            var filtered = string.IsNullOrWhiteSpace(SearchFilter)
                ? _allAvailableChannels
                : _allAvailableChannels.Where(c =>
                    c.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var c in filtered) AvailableChannels.Add(c);
        });
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

}

/// <summary>
/// Unified ViewModel for a channel in the single-list view.
/// Combines server data with local membership info.
/// </summary>
public partial class UnifiedChannelViewModel : ObservableObject
{
    // Read-only properties from server (ChannelResponse)
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string OwnerUsername { get; }
    public int MemberCount { get; }
    public int RuleCount { get; }
    public DateTime CreatedAt { get; }

    // Read-only properties from local membership (nullable when not joined)
    public string? LocalChannelId { get; }
    public DateTime? JoinedAt { get; }
    public DateTime? LastSyncedAt { get; }
    public int LocalRuleCount { get; }

    // Observable properties
    [ObservableProperty]
    private bool _isJoined;

    [ObservableProperty]
    private bool _isOwner;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private ObservableCollection<RulePreviewItem> _rulePreview = new();

    [ObservableProperty]
    private bool _isLoadingPreview;

    // Computed properties
    public string DisplayInfo => $"{MemberCount} members \u2022 {RuleCount} rules";
    public string OwnerDisplay => $"by {OwnerUsername}";
    public string JoinedInfo => IsJoined && LastSyncedAt.HasValue
        ? $"Last synced: {LastSyncedAt.Value:g}"
        : string.Empty;

    /// <summary>Two-letter monogram for the channel avatar (per channels.jsx V_Monogram).</summary>
    public string MonogramDisplay
    {
        get
        {
            var cleaned = (Name ?? string.Empty).Replace('-', ' ').Replace('_', ' ').Trim();
            if (string.IsNullOrEmpty(cleaned)) return "??";
            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
            }
            return cleaned.Length >= 2
                ? cleaned[..2].ToUpperInvariant()
                : cleaned.ToUpperInvariant();
        }
    }

    /// <summary>Relative-time display for last sync — always stays "Nx ago" framing.</summary>
    public string LastSyncDisplay
    {
        get
        {
            if (!LastSyncedAt.HasValue) return "never";
            var delta = DateTime.UtcNow - LastSyncedAt.Value;
            if (delta.TotalSeconds < 60) return "just now";
            if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
            if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
            if (delta.TotalDays < 30) return $"{(int)delta.TotalDays}d ago";
            if (delta.TotalDays < 365) return $"{(int)(delta.TotalDays / 30)}mo ago";
            return $"{(int)(delta.TotalDays / 365)}y ago";
        }
    }

    public UnifiedChannelViewModel(ChannelResponse response, ChannelMembershipDto? membership = null)
    {
        Id = response.Id;
        Name = response.Name;
        Description = response.Description;
        OwnerUsername = response.OwnerUsername;
        MemberCount = response.MemberCount;
        RuleCount = response.RuleCount;
        CreatedAt = response.CreatedAt;

        if (membership != null)
        {
            LocalChannelId = membership.ChannelId;
            JoinedAt = membership.JoinedAt;
            LastSyncedAt = membership.LastSyncedAt;
            LocalRuleCount = membership.RuleCount;
            IsJoined = true;
        }
    }
}

/// <summary>
/// Record for rule preview items in the expandable channel card.
/// </summary>
public record RulePreviewItem(Guid Id, Guid ChannelId, string Name, string Site, bool IsEnforced);

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

    public string DisplayInfo => $"{MemberCount} members \u2022 {RuleCount} rules";
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
