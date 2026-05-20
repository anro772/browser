using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Models;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

public partial class DownloadManagerViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<DownloadItemModel> _downloads = new();

    [ObservableProperty]
    private bool _isLoading;

    public DownloadManagerViewModel(IServiceScopeFactory scopeFactory, SettingsService settingsService)
    {
        _scopeFactory = scopeFactory;
        _settingsService = settingsService;
    }

    private string ResolveDownloadFolder()
    {
        var path = _settingsService.DefaultDownloadPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
        return path;
    }

    [RelayCommand]
    private void ChooseDownloadFolder()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Choose default download folder",
                InitialDirectory = ResolveDownloadFolder()
            };
            if (dialog.ShowDialog() == true)
            {
                _settingsService.DefaultDownloadPath = dialog.FolderName;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Choose download folder error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenDownloadsFolder()
    {
        try
        {
            var folder = ResolveDownloadFolder();
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            Process.Start("explorer.exe", folder);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Open downloads folder error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadDownloadsAsync()
    {
        IsLoading = true;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDownloadRepository>();
            var entities = await repo.GetAllAsync();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Downloads.Clear();
                foreach (var entity in entities)
                {
                    Downloads.Add(new DownloadItemModel
                    {
                        Id = entity.Id,
                        FileName = entity.FileName,
                        SourceUrl = entity.SourceUrl,
                        DestinationPath = entity.DestinationPath,
                        TotalBytes = entity.TotalBytes,
                        Status = entity.Status,
                        StartedAt = entity.StartedAt,
                        CompletedAt = entity.CompletedAt
                    });
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Download load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDownloadRepository>();
            await repo.ClearCompletedAsync();
            await LoadDownloadsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clear completed error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenFile(DownloadItemModel item)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.DestinationPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Open file error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowInFolder(DownloadItemModel item)
    {
        try
        {
            Process.Start("explorer.exe", $"/select,\"{item.DestinationPath}\"");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Show in folder error: {ex.Message}");
        }
    }

    public async Task AddDownload(string fileName, string sourceUrl, string destPath, long totalBytes)
    {
        var model = new DownloadItemModel
        {
            FileName = fileName,
            SourceUrl = sourceUrl,
            DestinationPath = destPath,
            TotalBytes = totalBytes,
            Status = "downloading",
            StartedAt = DateTime.UtcNow
        };

        Application.Current?.Dispatcher.Invoke(() =>
        {
            Downloads.Add(model);
        });

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDownloadRepository>();
            var entity = new DownloadEntity
            {
                FileName = fileName,
                SourceUrl = sourceUrl,
                DestinationPath = destPath,
                TotalBytes = totalBytes,
                Status = "downloading",
                StartedAt = DateTime.UtcNow
            };
            await repo.AddAsync(entity);
            model.Id = entity.Id;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Add download error: {ex.Message}");
        }
    }

    public void UpdateDownloadProgress(string destPath, long receivedBytes)
    {
        var item = Downloads.FirstOrDefault(d => d.DestinationPath == destPath);
        item?.UpdateProgress(receivedBytes);
    }

    public async Task CompleteDownload(string destPath, bool success)
    {
        var item = Downloads.FirstOrDefault(d => d.DestinationPath == destPath);
        if (item == null) return;

        item.Status = success ? "completed" : "failed";
        item.CompletedAt = DateTime.UtcNow;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDownloadRepository>();
            var entity = await repo.GetByPathAsync(destPath);
            if (entity != null)
            {
                entity.Status = item.Status;
                entity.CompletedAt = item.CompletedAt;
                await repo.UpdateAsync(entity);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Complete download error: {ex.Message}");
        }
    }
}
