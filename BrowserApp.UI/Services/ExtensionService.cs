using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.UI.Services;

public class ExtensionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private CoreWebView2Profile? _profile;
    private bool _profileReady;

    private static readonly string ExtractDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrowserApp", "Extensions");

    public ExtensionService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Sets the WebView2 profile for extension management. Must be called after the first tab initializes.
    /// </summary>
    public void SetProfile(CoreWebView2Profile profile)
    {
        _profile = profile;
        _profileReady = true;
    }

    /// <summary>
    /// Loads all enabled extensions from the database into the WebView2 profile on startup.
    /// </summary>
    public async Task LoadAllEnabledAsync()
    {
        if (!_profileReady || _profile == null) return;

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
        var extensions = await repo.GetAllAsync();

        foreach (var ext in extensions.Where(e => e.IsEnabled))
        {
            if (string.IsNullOrEmpty(ext.FolderPath) || !Directory.Exists(ext.FolderPath))
            {
                ErrorLogger.LogInfo($"[ExtensionService] Skipping extension '{ext.Name}' — folder not found: {ext.FolderPath}");
                continue;
            }

            try
            {
                await _profile.AddBrowserExtensionAsync(ext.FolderPath);
                ErrorLogger.LogInfo($"[ExtensionService] Loaded extension: {ext.Name}");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"[ExtensionService] Failed to load extension '{ext.Name}'", ex);
            }
        }
    }

    /// <summary>
    /// Gets all installed extensions, merging DB records with live WebView2 state.
    /// </summary>
    public async Task<IEnumerable<ExtensionEntity>> GetAllExtensionsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
        return await repo.GetAllAsync();
    }

    /// <summary>
    /// Installs an extension from an unpacked folder.
    /// </summary>
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

            // Load into WebView2 profile if available
            if (_profileReady && _profile != null)
            {
                try
                {
                    await _profile.AddBrowserExtensionAsync(folderPath);
                    ErrorLogger.LogInfo($"[ExtensionService] Installed extension into WebView2: {name}");
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError($"[ExtensionService] WebView2 extension install failed for '{name}'", ex);
                }
            }

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

    /// <summary>
    /// Installs an extension from a .crx file by extracting it first.
    /// CRX files are ZIP archives with a header that must be skipped.
    /// </summary>
    public async Task<ExtensionEntity?> InstallFromCrxAsync(string crxFilePath)
    {
        if (!File.Exists(crxFilePath))
            return null;

        try
        {
            Directory.CreateDirectory(ExtractDir);

            var crxBytes = await File.ReadAllBytesAsync(crxFilePath);
            var zipOffset = FindZipOffset(crxBytes);
            if (zipOffset < 0)
            {
                ErrorLogger.LogInfo($"[ExtensionService] Invalid CRX file: {crxFilePath}");
                return null;
            }

            // Extract to a temp directory first to read manifest
            var tempDir = Path.Combine(ExtractDir, $"_temp_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                using var zipStream = new MemoryStream(crxBytes, zipOffset, crxBytes.Length - zipOffset);
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(tempDir);

                // Read manifest to get extension name
                var manifestPath = Path.Combine(tempDir, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    Directory.Delete(tempDir, true);
                    return null;
                }

                var json = await File.ReadAllTextAsync(manifestPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Unknown" : "Unknown";

                // Move to final directory named after the extension
                var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                var finalDir = Path.Combine(ExtractDir, safeName);

                if (Directory.Exists(finalDir))
                    Directory.Delete(finalDir, true);

                Directory.Move(tempDir, finalDir);

                return await InstallExtensionAsync(finalDir);
            }
            catch
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                throw;
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"[ExtensionService] CRX install failed: {crxFilePath}", ex);
            return null;
        }
    }

    /// <summary>
    /// Uninstalls an extension from both WebView2 and the database.
    /// </summary>
    public async Task UninstallExtensionAsync(int id)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
        var all = await repo.GetAllAsync();
        var ext = all.FirstOrDefault(e => e.Id == id);

        // Remove from WebView2 profile
        if (_profileReady && _profile != null && ext != null)
        {
            try
            {
                var liveExtensions = await _profile.GetBrowserExtensionsAsync();
                var match = liveExtensions.FirstOrDefault(e =>
                    e.Name.Equals(ext.Name, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    await match.RemoveAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"[ExtensionService] WebView2 extension remove failed", ex);
            }
        }

        await repo.DeleteAsync(id);
    }

    /// <summary>
    /// Toggles an extension's enabled state in both WebView2 and the database.
    /// </summary>
    public async Task ToggleExtensionAsync(int id, bool enabled)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
        var all = await repo.GetAllAsync();
        var ext = all.FirstOrDefault(e => e.Id == id);
        if (ext == null) return;

        // Toggle in WebView2 profile
        if (_profileReady && _profile != null)
        {
            try
            {
                var liveExtensions = await _profile.GetBrowserExtensionsAsync();
                var match = liveExtensions.FirstOrDefault(e =>
                    e.Name.Equals(ext.Name, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    await match.EnableAsync(enabled);
                }
                else if (enabled && !string.IsNullOrEmpty(ext.FolderPath) && Directory.Exists(ext.FolderPath))
                {
                    // Re-add if enabling and not currently loaded
                    await _profile.AddBrowserExtensionAsync(ext.FolderPath);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"[ExtensionService] WebView2 toggle failed for '{ext.Name}'", ex);
            }
        }

        ext.IsEnabled = enabled;
        await repo.UpdateAsync(ext);
    }

    /// <summary>
    /// Finds the start of the ZIP data within a CRX file.
    /// CRX3 format: magic (4) + version (4) + header_length (4) + header + ZIP data
    /// CRX2 format: magic (4) + version (4) + pub_key_len (4) + sig_len (4) + pub_key + sig + ZIP data
    /// </summary>
    private static int FindZipOffset(byte[] data)
    {
        if (data.Length < 12) return -1;

        // Look for ZIP magic number (PK\x03\x04) anywhere in the first 1KB
        var zipMagic = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        for (int i = 0; i < Math.Min(data.Length - 4, 1024); i++)
        {
            if (data[i] == zipMagic[0] && data[i + 1] == zipMagic[1] &&
                data[i + 2] == zipMagic[2] && data[i + 3] == zipMagic[3])
            {
                return i;
            }
        }

        return -1;
    }
}
