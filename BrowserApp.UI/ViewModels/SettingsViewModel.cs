using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Models;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the settings dialog.
/// Manages user preferences including privacy mode and server configuration.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly IServiceScopeFactory _scopeFactory;

    [ObservableProperty]
    private PrivacyMode _selectedPrivacyMode;

    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private bool _isSaving;

    /// <summary>
    /// Available privacy modes for the dropdown.
    /// </summary>
    public IReadOnlyList<PrivacyModeOption> PrivacyModes { get; } = new List<PrivacyModeOption>
    {
        new(PrivacyMode.Relaxed, "Relaxed", "Minimal blocking - best for sites that break with aggressive blocking"),
        new(PrivacyMode.Standard, "Standard", "Balanced blocking - recommended for daily browsing"),
        new(PrivacyMode.Strict, "Strict", "Maximum blocking - may break some site functionality")
    };

    public SettingsViewModel(
        SettingsService settingsService,
        IServiceScopeFactory scopeFactory)
    {
        _settingsService = settingsService;
        _scopeFactory = scopeFactory;

        // Load current settings
        SelectedPrivacyMode = _settingsService.PrivacyMode;
        ServerUrl = _settingsService.ServerUrl;
    }

    /// <summary>
    /// Saves the current settings.
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        IsSaving = true;

        try
        {
            _settingsService.PrivacyMode = SelectedPrivacyMode;
            _settingsService.ServerUrl = ServerUrl;
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Clears all browsing history.
    /// </summary>
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all browsing history? This cannot be undone.",
            "Clear Browsing History",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var historyRepository = scope.ServiceProvider.GetRequiredService<IBrowsingHistoryRepository>();
                await historyRepository.ClearAllAsync();
                MessageBox.Show(
                    "Browsing history has been cleared.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to clear history: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Clears all network logs.
    /// </summary>
    [RelayCommand]
    private async Task ClearNetworkLogsAsync()
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all network logs? This cannot be undone.",
            "Clear Network Logs",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var networkLogRepository = scope.ServiceProvider.GetRequiredService<INetworkLogRepository>();
                await networkLogRepository.ClearAllAsync();
                MessageBox.Show(
                    "Network logs have been cleared.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to clear network logs: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    partial void OnSelectedPrivacyModeChanged(PrivacyMode value)
    {
        // Auto-save when privacy mode changes
        _settingsService.PrivacyMode = value;
    }

    partial void OnServerUrlChanged(string value)
    {
        // Auto-save when server URL changes
        _settingsService.ServerUrl = value;
    }
}

/// <summary>
/// Represents a privacy mode option for display in the UI.
/// </summary>
public record PrivacyModeOption(PrivacyMode Mode, string Name, string Description);
