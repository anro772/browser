using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Core.Services;
using BrowserApp.Data;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.Data.Repositories;
using BrowserApp.UI.Services;
using BrowserApp.UI.ViewModels;
using BrowserApp.UI.Views;
using BrowserApp.UI.Views.Workspaces;

namespace BrowserApp.UI;

/// <summary>
/// Interaction logic for App.xaml
/// Configures dependency injection and application startup.
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private ProfileService? _profileService;
    private string? _sessionLockPath;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ErrorLogger.LogInfo("Application starting");

        // Add global exception handler - log only, don't show dialogs
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            if (ex != null)
            {
                ErrorLogger.LogError("Unhandled Exception", ex);
            }
        };

        DispatcherUnhandledException += (s, args) =>
        {
            ErrorLogger.LogError("UI Thread Exception", args.Exception);
            args.Handled = true; // Prevent crash, just log it
        };

        try
        {
            await InitializeApplicationAsync();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Startup Failed", ex);

            System.Windows.MessageBox.Show(
                $"Application failed to start. Error logs written to:\n{ErrorLogger.GetLogDirectory()}\n\nError: {ex.Message}",
                "Startup Failed",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private async Task InitializeApplicationAsync()
    {
        // Initialize profile service BEFORE DI container
        _profileService = new ProfileService();
        _profileService.Initialize();

        // Set configurable paths based on active profile
        BrowserDbContext.SetDatabasePath(_profileService.GetDatabasePath());
        SettingsService.SetSettingsPath(_profileService.GetSettingsPath());

        ErrorLogger.LogInfo($"[Profile] Active: {_profileService.ActiveProfile.Name} ({_profileService.ActiveProfile.Id})");

        // Write crash-detection sentinel file
        _sessionLockPath = Path.Combine(Path.GetDirectoryName(_profileService.GetDatabasePath())!, "session.lock");

        // Sweep orphaned sentinels from other profile folders. They can only come from
        // prior runs on those profiles where OnExit didn't reach the sentinel-delete
        // (mostly historical — pre-async-void fix), and they'd cause a spurious
        // "didn't shut down properly" dialog if the user ever switched back to that
        // profile. The current profile's sentinel is written immediately after.
        try
        {
            var profilesRoot = Path.GetDirectoryName(Path.GetDirectoryName(_sessionLockPath));
            if (!string.IsNullOrEmpty(profilesRoot) && Directory.Exists(profilesRoot))
            {
                foreach (var orphan in Directory.EnumerateFiles(profilesRoot, "session.lock", SearchOption.AllDirectories))
                {
                    if (!string.Equals(orphan, _sessionLockPath, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(orphan); ErrorLogger.LogInfo($"[CrashRecovery] Swept orphan sentinel: {orphan}"); }
                        catch { /* best effort */ }
                    }
                }
            }
        }
        catch (Exception sweepEx)
        {
            ErrorLogger.LogError("[CrashRecovery] Orphan sweep failed", sweepEx);
        }

        File.WriteAllText(_sessionLockPath, DateTime.UtcNow.ToString("O"));
        ErrorLogger.LogInfo($"[CrashRecovery] Sentinel written: {_sessionLockPath}");

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        ErrorLogger.LogInfo("Services configured");

        // Ensure database is created — runs on a worker thread so the EF Core migration
        // doesn't pin the UI dispatcher during startup. We still await before showing
        // the window because subsequent service init reads from the DB.
        await Task.Run(EnsureDatabase);

        ErrorLogger.LogInfo("Database initialized");

        // First-run seed: populate the curated marketplace packs + channel(s) into
        // the local SQLite if those tables are empty. Idempotent — the seed service
        // short-circuits when data exists. Runs before BlockingService init so the
        // seeded rules are picked up on the initial rule load.
        await Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BrowserDbContext>();
            var seeder = new FirstRunSeedService(db);
            await seeder.SeedIfNeededAsync();
        });

        // Initialize blocking service (loads user rules) — small, fast, and the rule
        // engine is a hard dependency of every tab so this stays on the critical path.
        var blockingService = _serviceProvider.GetRequiredService<IBlockingService>();
        await blockingService.InitializeAsync();

        ErrorLogger.LogInfo("Blocking service initialized");

        // Initialize search engine from saved settings — cheap settings read.
        var settingsService = _serviceProvider.GetRequiredService<SettingsService>();
        settingsService.InitializeUsernameIfNeeded(_profileService.ActiveProfile.Name);

        var searchEngine = _serviceProvider.GetRequiredService<ISearchEngineService>();
        if (settingsService.SearchEngine == "Custom" && !string.IsNullOrWhiteSpace(settingsService.CustomSearchEngineUrl))
        {
            searchEngine.SetCustomSearchEngine(settingsService.CustomSearchEngineUrl);
        }
        else
        {
            searchEngine.SetSearchEngine(settingsService.SearchEngine);
        }

        ErrorLogger.LogInfo($"Search engine set to: {settingsService.SearchEngine}");

        // Show main window. The bookmarks bar populates from a per-profile JSON snapshot
        // synchronously in BookmarkViewModel's ctor (resolved as part of MainWindow), so
        // it paints with content on the first frame. Heavy services (filter list parse,
        // content policy, network logger, default-template seeding) continue on
        // background threads after this — none are required for the window to render,
        // and the active-profile pill reads its ABP state from a persisted snapshot.
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        ErrorLogger.LogInfo("Main window shown - kicking off background init");

        _ = Task.Run(async () =>
        {
            // Filter list parsing (~137k regex compiles). ShouldBlock returns false
            // safely while _isLoaded is false, so pages loaded in the first ~1 s simply
            // aren't filtered — silent, intentional.
            try
            {
                var filterListService = _serviceProvider.GetRequiredService<IFilterListService>();
                await filterListService.InitializeAsync();
                ErrorLogger.LogInfo($"Filter list service initialized: {filterListService.GetTotalFilterCount()} filters");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Filter list initialization failed (non-fatal)", ex);
            }

            // Category definitions for ContentPolicy rules — only used when a policy
            // category rule actually matches, so out-of-band init is fine.
            try
            {
                var contentPolicyService = _serviceProvider.GetRequiredService<ContentPolicyService>();
                await contentPolicyService.InitializeAsync();
                ErrorLogger.LogInfo("Content policy service initialized");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Content policy initialization failed (non-fatal)", ex);
            }

            // Network logger background channel — there are no active tabs yet when this
            // would have run on the critical path, so deferring loses nothing.
            try
            {
                var networkLogger = _serviceProvider.GetRequiredService<INetworkLogger>();
                await networkLogger.StartAsync();
                ErrorLogger.LogInfo("Network logger started");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Network logger start failed (non-fatal)", ex);
            }

            // First-run template seeding. Idempotent — short-circuits when rules exist.
            try
            {
                await AutoEnableDefaultTemplatesAsync(blockingService);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Default-template seeding failed (non-fatal)", ex);
            }

            // EF Core warmup. The first real query against BrowserDbContext pays a
            // ~100–200 ms tax (model snapshot init, connection open, LINQ provider JIT).
            // Doing one cheap query now means the History panel / autocomplete / etc. feel
            // snappier the first time the user actually opens them.
            try
            {
                using var scope = _serviceProvider!.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BrowserDbContext>();
                _ = await db.Bookmarks.CountAsync();
                ErrorLogger.LogInfo("EF Core warmup complete");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("EF Core warmup failed (non-fatal)", ex);
            }

            // Reconcile the bookmarks JSON snapshot against the DB so any drift (manual
            // edits, future sync) gets folded into the on-disk cache for next launch.
            try
            {
                var bookmarkVm = _serviceProvider!.GetRequiredService<ViewModels.BookmarkViewModel>();
                await bookmarkVm.LoadBookmarksAsync();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Bookmark reconcile failed (non-fatal)", ex);
            }
        });

        ErrorLogger.LogInfo("Startup complete (background init in flight)");
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        services.AddSingleton<IConfiguration>(config);

        // Core Services - SearchEngine is Singleton so setting persists
        services.AddSingleton<ISearchEngineService, SearchEngineService>();

        // Address-bar autocomplete enrichment — Google complete API
        services.AddSingleton<SearchSuggestionsService>();

        // Phase 3: Rule System Services
        services.AddSingleton<IRuleEngine>(sp => new RuleEngine(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ContentPolicyService>(),
            sp.GetRequiredService<SettingsService>()));
        services.AddSingleton<IBlockingService, BlockingService>();
        services.AddSingleton<CSSInjector>();
        services.AddSingleton<ICSSInjector>(sp => sp.GetRequiredService<CSSInjector>());
        services.AddSingleton<JSInjector>();
        services.AddSingleton<IJSInjector>(sp => sp.GetRequiredService<JSInjector>());

        // Filter List Service (EasyList/EasyPrivacy — cosmetic CSS only, blocking handled by AdBlock Plus)
        services.AddSingleton<IFilterListService, FilterListService>();

        // Content Policy Service
        services.AddSingleton<ContentPolicyService>();

        // Network Monitoring Services (Phase 2) - with blocking and content policy support
        services.AddSingleton<RequestInterceptor>(sp => new RequestInterceptor(
            sp.GetRequiredService<IBlockingService>(),
            sp.GetRequiredService<IFilterListService>(),
            sp.GetRequiredService<ContentPolicyService>()));
        services.AddSingleton<IRequestInterceptor>(sp => sp.GetRequiredService<RequestInterceptor>());
        services.AddSingleton<INetworkLogger, NetworkLogger>();

        // Phase 4: Marketplace Services
        services.AddSingleton<MarketplaceApiClient>();
        services.AddSingleton<IMarketplaceApiClient>(sp => sp.GetRequiredService<MarketplaceApiClient>());
        services.AddSingleton<RuleSyncService>();
        services.AddSingleton<IRuleSyncService>(sp => sp.GetRequiredService<RuleSyncService>());

        // Phase 5: Channel Services
        services.AddSingleton<ChannelApiClient>();
        services.AddSingleton<IChannelApiClient>(sp => sp.GetRequiredService<ChannelApiClient>());
        services.AddSingleton<ChannelSyncService>();
        services.AddSingleton<IChannelSyncService>(sp => sp.GetRequiredService<ChannelSyncService>());
        services.AddScoped<IChannelMembershipRepository, ChannelMembershipRepository>();

        // Navigation Service (kept for backward compat, but tabs handle their own nav now)
        services.AddSingleton<NavigationService>(sp => new NavigationService(
            sp.GetRequiredService<IRuleEngine>(),
            sp.GetRequiredService<ICSSInjector>(),
            sp.GetRequiredService<IJSInjector>()));
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

        // Data Services
        services.AddDbContext<BrowserDbContext>(options =>
        {
            string dbPath = BrowserDbContext.GetDatabasePath();
            options.UseSqlite($"Data Source={dbPath}");
        });
        services.AddScoped<IBrowsingHistoryRepository, BrowsingHistoryRepository>();
        services.AddScoped<INetworkLogRepository, NetworkLogRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();

        // Phase 9: Downloads
        services.AddScoped<IDownloadRepository, DownloadRepository>();
        services.AddSingleton<DownloadManagerViewModel>();

        // Phase 9: Extensions
        services.AddScoped<IExtensionRepository, ExtensionRepository>();
        services.AddSingleton<ExtensionService>();
        services.AddTransient<ExtensionManagerViewModel>();

        // Phase 6: AI/Ollama Services
        services.AddSingleton<IOllamaClient, OllamaClient>();
        services.AddSingleton<IRuleGenerationService, RuleGenerationService>();
        services.AddSingleton<IPageContextService, PageContextService>();

        // Settings Service
        services.AddSingleton<SettingsService>();

        // JSON snapshot of bookmarks. Read synchronously by BookmarkViewModel's ctor so
        // the bar paints with content on the first frame, without paying EF Core's cold
        // first-query tax. SQLite remains the source of truth.
        services.AddSingleton<BookmarkSnapshotService>();

        // Tails on-disk log files for the debug console.
        services.AddSingleton<LogTailService>();

        // Profile Service (already initialized before DI)
        services.AddSingleton(_profileService!);
        services.AddSingleton<ProfileSelectorViewModel>();

        // Phase 7: Tab System
        services.AddSingleton<TabStripViewModel>();
        services.AddTransient<NewTabPageViewModel>();
        services.AddSingleton<BookmarkViewModel>();

        // Phase 6: AI ViewModels
        services.AddSingleton<CopilotSidebarViewModel>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<NetworkMonitorViewModel>();
        services.AddTransient<RuleManagerViewModel>();
        services.AddSingleton<LogViewerViewModel>();
        services.AddTransient<ChannelsViewModel>();
        services.AddTransient<MarketplaceViewModel>();
        services.AddSingleton<PrivacyDashboardViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
        services.AddSingleton<NetworkMonitorView>();
        services.AddTransient<RuleManagerView>();
        services.AddSingleton<LogViewerView>();
        services.AddTransient<ChannelsView>();
        services.AddTransient<MarketplaceView>();
        services.AddSingleton<PrivacyDashboardView>();
        services.AddSingleton<HistoryView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<ProfileSelectorView>();
        services.AddTransient<NewTabPageView>();
        services.AddSingleton<WorkspaceHostView>();
        services.AddTransient<RulesWorkspaceView>();
        services.AddTransient<ExtensionsWorkspaceView>();
        services.AddTransient<MarketplaceWorkspaceView>();
        services.AddTransient<ChannelsWorkspaceView>();
        services.AddTransient<ProfilesWorkspaceView>();
        services.AddTransient<SettingsWorkspaceView>();

        // Phase 6: AI Views
        services.AddSingleton<CopilotSidebarView>();

        // Phase 9: New Views
        services.AddSingleton<DownloadManagerView>();
        services.AddTransient<ExtensionManagerView>();
    }

    private async Task AutoEnableDefaultTemplatesAsync(IBlockingService blockingService)
    {
        try
        {
            using var scope = _serviceProvider!.CreateScope();
            var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
            var templateNames = new[] { "cookie-banners", "privacy-headers", "block-trackers" };
            var assembly = Assembly.GetExecutingAssembly();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var templateName in templateNames)
            {
                string? json = null;
                var resourceName = $"BrowserApp.UI.Resources.DefaultRules.{templateName}.json";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    json = await reader.ReadToEndAsync();
                }
                else
                {
                    var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "DefaultRules", $"{templateName}.json");
                    if (File.Exists(filePath))
                        json = await File.ReadAllTextAsync(filePath);
                }

                if (string.IsNullOrEmpty(json)) continue;

                var rule = JsonSerializer.Deserialize<Rule>(json, jsonOptions);
                if (rule == null) continue;

                if (await ruleRepo.ExistsAsync(rule.Id)) continue;

                var entity = new RuleEntity
                {
                    Id = rule.Id,
                    Name = rule.Name,
                    Description = rule.Description,
                    Site = rule.Site,
                    Enabled = true,
                    Priority = rule.Priority,
                    RulesJson = JsonSerializer.Serialize(rule.Rules, jsonOptions),
                    Source = "template",
                    IsEnforced = false
                };

                await ruleRepo.AddAsync(entity);
                ErrorLogger.LogInfo($"[AutoTemplates] Loaded template: {rule.Name}");
            }

            // Reload rules so the blocking service picks them up
            await blockingService.InitializeAsync();
            ErrorLogger.LogInfo("[AutoTemplates] Default templates loaded and blocking service refreshed");
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("[AutoTemplates] Failed to auto-enable default templates (non-fatal)", ex);
        }
    }

    private void EnsureDatabase()
    {
        using var scope = _serviceProvider!.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BrowserDbContext>();

        try
        {
            ErrorLogger.LogInfo("Running EF Core migrations");
            // Use EF Core migrations for proper schema management
            dbContext.Database.Migrate();
            ErrorLogger.LogInfo("Migrations completed successfully");
        }
        catch (Exception ex)
        {
            // Log the error
            ErrorLogger.LogError("Database migration failed", ex);

            string dbPath = BrowserDbContext.GetDatabasePath();
            string backupPath = $"{dbPath}.backup_{DateTime.Now:yyyyMMddHHmmss}";

            try
            {
                // Create backup before attempting recovery
                if (File.Exists(dbPath))
                {
                    ErrorLogger.LogInfo($"Creating database backup at: {backupPath}");
                    File.Copy(dbPath, backupPath, overwrite: true);
                }

                ErrorLogger.LogInfo("Attempting to recreate database with migrations");
                // Database exists but was created without migrations (legacy)
                // Delete and recreate with proper migrations
                dbContext.Database.EnsureDeleted();
                dbContext.Database.Migrate();
                ErrorLogger.LogInfo("Database recreated successfully. Backup saved at: " + backupPath);
            }
            catch (Exception innerEx)
            {
                ErrorLogger.LogError("Database recreation failed", innerEx);

                // Restore from backup if recreation failed
                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Copy(backupPath, dbPath, overwrite: true);
                        ErrorLogger.LogInfo("Database restored from backup");
                    }
                    catch (Exception restoreEx)
                    {
                        ErrorLogger.LogError("Failed to restore backup", restoreEx);
                    }
                }

                throw;
            }
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // ── Delete crash-detection sentinel FIRST, synchronously.
        // OnExit is async void, which WPF does not await — anything after the first
        // `await` below may not finish before the process tears down (the WebView2
        // dispose path can stall long enough to kill the continuation). Removing the
        // sentinel here means "the user asked to close", which is the signal we
        // actually want; the post-await work is best-effort cleanup.
        if (_sessionLockPath != null && File.Exists(_sessionLockPath))
        {
            try
            {
                File.Delete(_sessionLockPath);
                ErrorLogger.LogInfo("[CrashRecovery] Sentinel deleted on clean exit");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Failed to delete session lock", ex);
            }
        }

        if (_serviceProvider != null)
        {
            // Save tab session before exit
            try
            {
                var tabStrip = _serviceProvider.GetService<TabStripViewModel>();
                var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
                if (tabStrip != null)
                {
                    await tabStrip.SaveSessionAsync(scopeFactory);
                    ErrorLogger.LogInfo("Tab session saved on exit");
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Failed to save tab session on exit", ex);
            }

            // Stop network logger gracefully
            var networkLogger = _serviceProvider.GetService<INetworkLogger>();
            if (networkLogger != null)
            {
                await networkLogger.DisposeAsync();
            }

            // Dispose tab strip
            var strip = _serviceProvider.GetService<TabStripViewModel>();
            strip?.Dispose();
        }

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}
