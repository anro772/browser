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
    private readonly SettingsService? _settingsService;
    private CoreWebView2Profile? _profile;
    private bool _profileReady;

    private static readonly string ExtractDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrowserApp", "Extensions");

    private static readonly string BuiltInExtensionsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrowserApp", "Extensions");

    public ExtensionService(IServiceScopeFactory scopeFactory, SettingsService? settingsService = null)
    {
        _scopeFactory = scopeFactory;
        _settingsService = settingsService;
    }

    /// <summary>
    /// Raised whenever the built-in ad blocker's enabled state changes (toggle, startup
    /// sync, migration). Chrome elements (e.g. the active-profile pill's shield indicator)
    /// subscribe here to refresh live without polling.
    /// </summary>
    public event EventHandler<bool>? AdBlockerStateChanged;

    private void RaiseAdBlockerStateChanged(bool enabled)
    {
        try { AdBlockerStateChanged?.Invoke(this, enabled); }
        catch (Exception ex) { ErrorLogger.LogError("[ExtensionService] AdBlockerStateChanged handler threw", ex); }
    }

    /// <summary>
    /// Ensures built-in extensions are installed. Promotes user-imported adblock extensions
    /// to built-in status, or installs from bundled resources if needed.
    /// </summary>
    public async Task EnsureBuiltInExtensionsAsync()
    {
        if (!_profileReady || _profile == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
            var extensions = (await repo.GetAllAsync()).ToList();

            // ── De-dupe: prior bug stored the unresolved manifest placeholder
            // "__MSG_name_releasebuild__" as the name, which broke the "already
            // registered" check and let us add a new built-in record on every restart.
            // Collapse any duplicates to a single record (preferring one whose
            // FolderPath still resolves) and delete the rest.
            var builtIns = extensions.Where(e => e.IsBuiltIn).ToList();
            if (builtIns.Count > 1)
            {
                var keeper = builtIns
                    .OrderByDescending(e => !string.IsNullOrEmpty(e.FolderPath) && Directory.Exists(e.FolderPath))
                    .ThenBy(e => e.Id)
                    .First();
                foreach (var dup in builtIns.Where(e => e.Id != keeper.Id))
                {
                    await repo.DeleteAsync(dup.Id);
                    ErrorLogger.LogInfo($"[ExtensionService] Removed duplicate built-in record: '{dup.Name}' (Id={dup.Id})");
                }
                extensions = (await repo.GetAllAsync()).ToList();
            }

            // Any IsBuiltIn record counts — we only ever register one. Avoids
            // name-substring matching that could miss future ad blockers.
            var existingBuiltIn = extensions.FirstOrDefault(e => e.IsBuiltIn);

            // One-shot migration: FilterListService is now the primary blocker. If the
            // built-in ABP is currently enabled, disable it once so the user gets a
            // clean comparison. The flag ensures we don't fight the user's later choice.
            if (existingBuiltIn != null && _settingsService != null
                && !_settingsService.HasMigratedToFilterListPrimary)
            {
                if (existingBuiltIn.IsEnabled)
                {
                    existingBuiltIn.IsEnabled = false;
                    await repo.UpdateAsync(existingBuiltIn);
                    ErrorLogger.LogInfo($"[ExtensionService] One-shot migration: disabled built-in ad blocker so FilterListService is the sole blocker. Re-enable via Settings if desired.");
                }
                _settingsService.HasMigratedToFilterListPrimary = true;
            }

            if (existingBuiltIn != null)
            {
                // The recorded folder may have been deleted between sessions (e.g. user
                // cleared LocalAppData\BrowserApp\Extensions). Re-materialize it from the
                // bundled source so the extension keeps working without losing the DB row.
                if (string.IsNullOrEmpty(existingBuiltIn.FolderPath) || !Directory.Exists(existingBuiltIn.FolderPath))
                {
                    var bundledSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "Resources", "BuiltInExtensions", "adblock-plus");
                    var rematerializeTarget = Path.IsPathRooted(existingBuiltIn.FolderPath ?? "")
                        ? existingBuiltIn.FolderPath!
                        : Path.Combine(BuiltInExtensionsDir, "adblock-plus");

                    if (Directory.Exists(bundledSource))
                    {
                        try
                        {
                            Directory.CreateDirectory(BuiltInExtensionsDir);
                            if (!Directory.Exists(rematerializeTarget))
                            {
                                CopyDirectory(bundledSource, rematerializeTarget);
                                ErrorLogger.LogInfo($"[ExtensionService] Re-materialized missing built-in folder: {rematerializeTarget}");
                            }
                            if (existingBuiltIn.FolderPath != rematerializeTarget)
                            {
                                existingBuiltIn.FolderPath = rematerializeTarget;
                                await repo.UpdateAsync(existingBuiltIn);
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorLogger.LogError($"[ExtensionService] Failed to re-materialize built-in from bundled source", ex);
                        }
                    }
                    else
                    {
                        ErrorLogger.LogInfo($"[ExtensionService] Built-in folder missing and no bundled source at {bundledSource} — extension is unrecoverable until reinstall");
                    }
                }

                // Repair a stored "__MSG_..." placeholder name from earlier launches by
                // resolving it against the on-disk _locales/<default>/messages.json.
                if (!string.IsNullOrEmpty(existingBuiltIn.FolderPath) && Directory.Exists(existingBuiltIn.FolderPath)
                    && (existingBuiltIn.Name.StartsWith("__MSG_") || string.IsNullOrWhiteSpace(existingBuiltIn.Name)))
                {
                    var (resolved, _) = await ReadManifestNameAndVersionAsync(existingBuiltIn.FolderPath, fallbackName: existingBuiltIn.Name);
                    if (!string.IsNullOrEmpty(resolved) && resolved != existingBuiltIn.Name)
                    {
                        ErrorLogger.LogInfo($"[ExtensionService] Repaired built-in name: '{existingBuiltIn.Name}' → '{resolved}'");
                        existingBuiltIn.Name = resolved;
                        await repo.UpdateAsync(existingBuiltIn);
                    }
                }

                // Sync the WebView2 side with the DB state. We no longer force-enable —
                // the user's toggle in Settings is the source of truth. If DB says
                // enabled, we make sure WebView2 has it loaded and enabled; if DB says
                // disabled, we explicitly disable it in WebView2 (it may have been
                // cached as enabled from a prior session).
                try
                {
                    var live = await _profile.GetBrowserExtensionsAsync();
                    var match = live.FirstOrDefault(e => e.Name.Equals(existingBuiltIn.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingBuiltIn.IsEnabled)
                    {
                        if (match == null && !string.IsNullOrEmpty(existingBuiltIn.FolderPath) && Directory.Exists(existingBuiltIn.FolderPath))
                        {
                            await _profile.AddBrowserExtensionAsync(existingBuiltIn.FolderPath);
                            live = await _profile.GetBrowserExtensionsAsync();
                            match = live.FirstOrDefault(e => e.Name.Equals(existingBuiltIn.Name, StringComparison.OrdinalIgnoreCase));
                        }
                        if (match != null)
                        {
                            await match.EnableAsync(true);
                        }
                    }
                    else if (match != null)
                    {
                        await match.EnableAsync(false);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError($"[ExtensionService] Failed to sync built-in WebView2 state with DB", ex);
                }

                ErrorLogger.LogInfo($"[ExtensionService] Built-in ad blocker already registered: {existingBuiltIn.Name} ({(existingBuiltIn.IsEnabled ? "enabled" : "disabled")})");
                RaiseAdBlockerStateChanged(existingBuiltIn.IsEnabled);
                return;
            }

            // Check if user already imported an ad blocker extension (from .crx) — promote it to built-in
            var importedAdblock = extensions.FirstOrDefault(e => !e.IsBuiltIn &&
                (e.Name.Contains("Adblock", StringComparison.OrdinalIgnoreCase) ||
                 e.Name.Contains("adblock", StringComparison.OrdinalIgnoreCase) ||
                 e.Name.Contains("uBlock", StringComparison.OrdinalIgnoreCase)));

            if (importedAdblock != null)
            {
                importedAdblock.IsBuiltIn = true;
                importedAdblock.IsEnabled = true;
                await repo.UpdateAsync(importedAdblock);
                ErrorLogger.LogInfo($"[ExtensionService] Promoted imported ad blocker to built-in: {importedAdblock.Name}");
                return;
            }

            // Fall back to bundled resources
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var builtInSource = Path.Combine(basePath, "Resources", "BuiltInExtensions", "adblock-plus");

            if (!Directory.Exists(builtInSource))
            {
                ErrorLogger.LogInfo("[ExtensionService] No ad blocker found — install one from Extensions to enable built-in ad blocking");
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

            var (name, version) = await ReadManifestNameAndVersionAsync(targetDir, fallbackName: "Adblock Plus");

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

    /// <summary>
    /// Reads (name, version) from a Chrome extension manifest, resolving any
    /// "__MSG_key__" localization placeholder against _locales/&lt;default_locale&gt;/messages.json.
    /// Returns the fallback name if the manifest is missing or unreadable.
    /// </summary>
    private static async Task<(string Name, string Version)> ReadManifestNameAndVersionAsync(string folder, string fallbackName)
    {
        var manifestPath = Path.Combine(folder, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            return (fallbackName, "0.0");
        }

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var rawName = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? fallbackName : fallbackName;
            var version = root.TryGetProperty("version", out var verProp) ? verProp.GetString() ?? "0.0" : "0.0";
            var defaultLocale = root.TryGetProperty("default_locale", out var locProp) ? locProp.GetString() : null;

            var resolved = await ResolveLocalizedMessageAsync(folder, rawName, defaultLocale) ?? rawName;
            return (resolved, version);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError($"[ExtensionService] Failed to read manifest at {manifestPath}", ex);
            return (fallbackName, "0.0");
        }
    }

    /// <summary>
    /// If <paramref name="raw"/> is a "__MSG_key__" placeholder, look it up in the extension's
    /// default-locale messages file and return the resolved string. Otherwise return null.
    /// </summary>
    private static async Task<string?> ResolveLocalizedMessageAsync(string folder, string raw, string? defaultLocale)
    {
        if (string.IsNullOrEmpty(raw) || !raw.StartsWith("__MSG_") || !raw.EndsWith("__")) return null;
        var key = raw.Substring("__MSG_".Length, raw.Length - "__MSG_".Length - "__".Length);
        if (string.IsNullOrEmpty(key)) return null;

        var locale = string.IsNullOrEmpty(defaultLocale) ? "en_US" : defaultLocale;
        var candidates = new[] { locale, locale.Replace('-', '_'), "en_US", "en" };
        foreach (var c in candidates.Distinct())
        {
            var messagesPath = Path.Combine(folder, "_locales", c, "messages.json");
            if (!File.Exists(messagesPath)) continue;

            try
            {
                var json = await File.ReadAllTextAsync(messagesPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(key, out var entry) &&
                    entry.TryGetProperty("message", out var msg))
                {
                    var resolved = msg.GetString();
                    if (!string.IsNullOrEmpty(resolved)) return resolved;
                }
            }
            catch
            {
                // try next candidate
            }
        }
        return null;
    }

    /// <summary>
    /// Gets whether the built-in ad blocker extension is currently enabled.
    /// </summary>
    public async Task<bool> IsAdBlockerEnabledAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
        var extensions = await repo.GetAllAsync();
        var adblock = extensions.FirstOrDefault(e => e.IsBuiltIn);
        return adblock?.IsEnabled ?? false;
    }

    /// <summary>
    /// Toggles the built-in ad blocker extension on/off. If the built-in isn't yet
    /// registered (e.g. user toggled before the first tab triggered EnsureBuiltIn),
    /// we attempt registration first so the toggle isn't silently lost.
    /// </summary>
    public async Task SetAdBlockerEnabledAsync(bool enabled)
    {
        ExtensionEntity? adblock;

        using (var scope = _scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
            adblock = (await repo.GetAllAsync()).FirstOrDefault(e => e.IsBuiltIn);
        }

        if (adblock == null)
        {
            // Try to register/promote the built-in now (no-op if the WebView2 profile
            // isn't ready yet — but at least the DB record will be created next time).
            await EnsureBuiltInExtensionsAsync();
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IExtensionRepository>();
            adblock = (await repo.GetAllAsync()).FirstOrDefault(e => e.IsBuiltIn);
        }

        if (adblock == null)
        {
            ErrorLogger.LogInfo($"[ExtensionService] Cannot {(enabled ? "enable" : "disable")} ad blocker — no built-in registered yet (profile not ready?)");
            return;
        }

        await ToggleExtensionAsync(adblock.Id, enabled);
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
    /// Loads all DB-enabled extensions into the WebView2 profile on startup.
    /// WebView2 persists per-extension on/off state in its profile data, so if the user
    /// (or a previous session) disabled an extension at the WebView2 level, AddBrowserExtensionAsync
    /// alone won't re-enable it. Always re-resolve the live match and force EnableAsync(true).
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
                var live = await _profile.GetBrowserExtensionsAsync();
                var match = live.FirstOrDefault(e => e.Name.Equals(ext.Name, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    await _profile.AddBrowserExtensionAsync(ext.FolderPath);
                    ErrorLogger.LogInfo($"[ExtensionService] Added extension at startup: {ext.Name}");
                    live = await _profile.GetBrowserExtensionsAsync();
                    match = live.FirstOrDefault(e => e.Name.Equals(ext.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (match != null)
                {
                    await match.EnableAsync(true);
                    ErrorLogger.LogInfo($"[ExtensionService] Loaded extension at startup (DB said enabled): {ext.Name}");
                }
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
            var (name, version) = await ReadManifestNameAndVersionAsync(folderPath, fallbackName: "Unknown");

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

                if (match == null && enabled && !string.IsNullOrEmpty(ext.FolderPath) && Directory.Exists(ext.FolderPath))
                {
                    // Not loaded yet — add, then re-resolve so we can explicitly enable
                    // (AddBrowserExtensionAsync alone may attach it in WebView2's last-known
                    // state, which can be disabled across sessions).
                    await _profile.AddBrowserExtensionAsync(ext.FolderPath);
                    liveExtensions = await _profile.GetBrowserExtensionsAsync();
                    match = liveExtensions.FirstOrDefault(e =>
                        e.Name.Equals(ext.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (match != null)
                {
                    await match.EnableAsync(enabled);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError($"[ExtensionService] WebView2 toggle failed for '{ext.Name}'", ex);
            }
        }

        ext.IsEnabled = enabled;
        await repo.UpdateAsync(ext);

        // Notify chrome listeners (active-profile pill shield indicator etc.) when the
        // built-in ad blocker's state changes.
        if (ext.IsBuiltIn)
        {
            RaiseAdBlockerStateChanged(enabled);
        }
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
