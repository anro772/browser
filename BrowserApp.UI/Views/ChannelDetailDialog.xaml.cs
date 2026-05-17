using System;
using System.ComponentModel;
using System.Windows;
using BrowserApp.UI.ViewModels;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

public partial class ChannelDetailDialog : FluentWindow, INotifyPropertyChanged
{
    public UnifiedChannelViewModel Channel { get; }
    private readonly ChannelsViewModel _parentVm;

    public string HeaderName => $"#{Channel.Name}";

    public string RuleCountDisplay => Channel.IsJoined
        ? Channel.RulePreview.Count.ToString()
        : Channel.RuleCount.ToString();

    public string CreatedDisplay => Channel.CreatedAt == default
        ? "—"
        : Channel.CreatedAt.ToString("yyyy-MM-dd");

    public string JoinedDisplay => Channel.JoinedAt.HasValue
        ? Channel.JoinedAt.Value.ToString("yyyy-MM-dd")
        : "Not joined";

    public Visibility RulesEmptyVisibility =>
        (!Channel.IsLoadingPreview && Channel.IsJoined && Channel.RulePreview.Count == 0)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility NotJoinedVisibility =>
        Channel.IsJoined ? Visibility.Collapsed : Visibility.Visible;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ChannelDetailDialog(UnifiedChannelViewModel channel, ChannelsViewModel parentVm)
    {
        Channel = channel;
        _parentVm = parentVm;
        DataContext = this;
        InitializeComponent();

        Channel.PropertyChanged += OnChannelPropertyChanged;
        Channel.RulePreview.CollectionChanged += (_, _) =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RuleCountDisplay)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RulesEmptyVisibility)));
        };

        Loaded += async (_, _) =>
        {
            if (Channel.IsJoined && Channel.RulePreview.Count == 0)
            {
                await _parentVm.LoadChannelRulesAsync(Channel);
            }
        };

        Closed += (_, _) => Channel.PropertyChanged -= OnChannelPropertyChanged;
    }

    private void OnChannelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UnifiedChannelViewModel.IsLoadingPreview))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RulesEmptyVisibility)));
        }
    }

    private async void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_parentVm.AddRuleToChannelCommand.CanExecute(Channel))
        {
            await _parentVm.AddRuleToChannelCommand.ExecuteAsync(Channel);
            // Refresh display
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RuleCountDisplay)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RulesEmptyVisibility)));
        }
    }

    private async void DeleteRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is RulePreviewItem rule
            && _parentVm.DeleteChannelRuleCommand.CanExecute(rule))
        {
            await _parentVm.DeleteChannelRuleCommand.ExecuteAsync(rule);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RuleCountDisplay)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RulesEmptyVisibility)));
        }
    }

    private async void SyncNowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_parentVm.SyncChannelCommand.CanExecute(Channel))
        {
            await _parentVm.SyncChannelCommand.ExecuteAsync(Channel);
            // Reload rules into the dialog
            await _parentVm.LoadChannelRulesAsync(Channel);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RuleCountDisplay)));
        }
    }

    private async void LeaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_parentVm.LeaveChannelCommand.CanExecute(Channel))
        {
            await _parentVm.LeaveChannelCommand.ExecuteAsync(Channel);
            // Close regardless — LoadChannelsAsync rebuilt _allChannels with new VMs,
            // so our reference is stale. Reopen via the workspace if still wanted.
            Close();
        }
    }
}
