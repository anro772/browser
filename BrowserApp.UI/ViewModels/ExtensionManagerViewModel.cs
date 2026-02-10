using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Data.Entities;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

public partial class ExtensionManagerViewModel : ObservableObject
{
    private readonly ExtensionService _extensionService;

    [ObservableProperty]
    private ObservableCollection<ExtensionEntity> _extensions = new();

    [ObservableProperty]
    private bool _isLoading;

    public ExtensionManagerViewModel(ExtensionService extensionService)
    {
        _extensionService = extensionService;
    }

    [RelayCommand]
    private async Task LoadExtensionsAsync()
    {
        IsLoading = true;

        try
        {
            var all = await _extensionService.GetAllExtensionsAsync();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Extensions.Clear();
                foreach (var ext in all)
                {
                    Extensions.Add(ext);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Extension load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task InstallExtensionAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Extension Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                var result = await _extensionService.InstallExtensionAsync(dialog.FolderName);
                if (result != null)
                {
                    await LoadExtensionsAsync();
                }
                else
                {
                    MessageBox.Show(
                        "Failed to install extension. Make sure the folder contains a valid manifest.json.",
                        "Install Extension",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Extension install error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task UninstallExtensionAsync(ExtensionEntity ext)
    {
        try
        {
            await _extensionService.UninstallExtensionAsync(ext.Id);
            await LoadExtensionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Extension uninstall error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleExtensionAsync(ExtensionEntity ext)
    {
        try
        {
            await _extensionService.ToggleExtensionAsync(ext.Id, !ext.IsEnabled);
            await LoadExtensionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Extension toggle error: {ex.Message}");
        }
    }
}
