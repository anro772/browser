using System.IO;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.UI.Services;

public class ExtensionService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ExtensionService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<IEnumerable<ExtensionEntity>> GetAllExtensionsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
        return await repo.GetAllAsync();
    }

    public async Task<ExtensionEntity?> InstallExtensionAsync(string folderPath)
    {
        var manifestPath = Path.Combine(folderPath, "manifest.json");
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Unknown" : "Unknown";
            var version = root.TryGetProperty("version", out var verProp) ? verProp.GetString() ?? "0.0" : "0.0";

            var entity = new ExtensionEntity
            {
                Name = name,
                Version = version,
                FolderPath = folderPath,
                IsEnabled = true,
                InstalledAt = DateTime.UtcNow
            };

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
            await repo.AddAsync(entity);
            return entity;
        }
        catch
        {
            return null;
        }
    }

    public async Task UninstallExtensionAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
        await repo.DeleteAsync(id);
    }

    public async Task ToggleExtensionAsync(int id, bool enabled)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
        var all = await repo.GetAllAsync();
        var ext = all.FirstOrDefault(e => e.Id == id);
        if (ext != null)
        {
            ext.IsEnabled = enabled;
            await repo.UpdateAsync(ext);
        }
    }
}
