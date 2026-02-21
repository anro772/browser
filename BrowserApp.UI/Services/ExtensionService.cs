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

    private static readonly string BuiltInExtensionsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrowserApp", "Extensions");

    public ExtensionService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Ensures built-in extensions are installed. Copies from embedded resources if needed.
    /// </summary>
    public async Task EnsureBuiltInExtensionsAsync()
    {
        if (!_profileReady || _profile == null) return;

        try
        {
            // Check if adblock-plus is already in the database as built-in
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
            var extensions = await repo.GetAllAsync();
            var existingBuiltIn = extensions.FirstOrDefault(e => e.IsBuiltIn && e.Name.Contains("Adblock Plus", StringComparison.OrdinalIgnoreCase));

            if (existingBuiltIn != null)
            {
                ErrorLogger.LogInfo("[ExtensionService] Built-in Adblock Plus already installed");
                return;
            }

            // Look for bundled extension in resources
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var builtInSource = Path.Combine(basePath, "Resources", "BuiltInExtensions", "adblock-plus");

            if (!Directory.Exists(builtInSource))
            {
                ErrorLogger.LogInfo("[ExtensionService] Built-in Adblock Plus source not found — skipping auto-install");
                return;
            }

            // Copy to extensions directory
            Directory.CreateDirectory(BuiltInExtensionsDir);
            var targetDir = Path.Combine(BuiltInExtensionsDir, "adblock-plus");

            if (!Directory.Exists(targetDir))
            {
                CopyDirectory(builtInSource, targetDir);
                ErrorLogger.LogInfo($"[ExtensionService] Copied built-in Adblock Plus to: {targetDir}");
            }

            // Install and mark as built-in
            var manifestPath = Path.Combine(targetDir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                ErrorLogger.LogInfo("[ExtensionService] Built-in Adblock Plus manifest not found after copy");
                return;
            }

            var json = await File.ReadAllTextAsync(manifestPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Adblock Plus" : "Adblock Plus";
            var version = root.TryGetProperty("version", out var verProp) ? verProp.GetString() ?? "0.0" : "0.0";

            // Load into WebView2
            try
            {
                await _profile.AddBrowserExtensionAsync(targetDir);
                ErrorLogger.LogInfo($"[ExtensionService] Built-in extension loaded into WebView2: {name}");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"[ExtensionService] Failed to load built-in extension into WebView2", ex);
            }

            var entity = new ExtensionEntity
            {
                Name = name,
                Version = version,
                FolderPath = targetDir,
                IsEnabled = true,
                IsBuiltIn = true,
                InstalledAt = DateTime.UtcNow
            };

            await repo.AddAsync(entity);
            ErrorLogger.LogInfo($"[ExtensionService] Built-in extension registered: {name} v{version}");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("[ExtensionService] Failed to ensure built-in extensions", ex);
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
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
                ErrorLogger.LogInfo($"[ExtensionService] Invalid CRX file (no ZIP data found): {crxFilePath} ({crxBytes.Length} bytes)");
                return null;
            }

            ErrorLogger.LogInfo($"[ExtensionService] CRX file: {Path.GetFileName(crxFilePath)}, size: {crxBytes.Length}, ZIP offset: {zipOffset}");

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

        // Prevent uninstalling built-in extensions
        if (ext?.IsBuiltIn == true)
        {
            ErrorLogger.LogInfo($"[ExtensionService] Cannot uninstall built-in extension: {ext.Name}");
            return;
        }

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
    /// Finds the start of the ZIP data within a CRX file by parsing the header format.
    /// CRX3 format: "Cr24" (4) + version uint32 (4) + header_length uint32 (4) + header bytes + ZIP data
    /// CRX2 format: "Cr24" (4) + version uint32 (4) + pub_key_len uint32 (4) + sig_len uint32 (4) + pub_key + sig + ZIP data
    /// </summary>
    private static int FindZipOffset(byte[] data)
    {
        if (data.Length < 16) return -1;

        // Check for CRX magic "Cr24"
        bool isCrx = data[0] == 0x43 && data[1] == 0x72 && data[2] == 0x32 && data[3] == 0x34;

        if (isCrx)
        {
            uint version = BitConverter.ToUInt32(data, 4);

            if (version == 3)
            {
                // CRX3: 12-byte fixed header + variable header
                uint headerLength = BitConverter.ToUInt32(data, 8);
                int offset = 12 + (int)headerLength;
                if (offset + 4 <= data.Length &&
                    data[offset] == 0x50 && data[offset + 1] == 0x4B)
                    return offset;
            }
            else if (version == 2)
            {
                // CRX2: 16-byte fixed header + pub_key + signature
                uint pubKeyLen = BitConverter.ToUInt32(data, 8);
                uint sigLen = BitConverter.ToUInt32(data, 12);
                int offset = 16 + (int)pubKeyLen + (int)sigLen;
                if (offset + 4 <= data.Length &&
                    data[offset] == 0x50 && data[offset + 1] == 0x4B)
                    return offset;
            }
        }

        // Fallback: scan for ZIP magic (PK\x03\x04) in the first 64KB
        for (int i = 0; i < Math.Min(data.Length - 4, 65536); i++)
        {
            if (data[i] == 0x50 && data[i + 1] == 0x4B &&
                data[i + 2] == 0x03 && data[i + 3] == 0x04)
            {
                return i;
            }
        }

        return -1;
    }
}
