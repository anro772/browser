using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Services;
using BrowserApp.Data;
using BrowserApp.Data.Interfaces;
using BrowserApp.Data.Repositories;
using BrowserApp.UI.Services;
using BrowserApp.UI.ViewModels;
using BrowserApp.UI.Views;

namespace BrowserApp.UI;

/// <summary>
/// Interaction logic for App.xaml
/// Configures dependency injection and application startup.
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private ProfileService? _profileService;

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

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        ErrorLogger.LogInfo("Services configured");

        // Ensure database is created
        EnsureDatabase();

        ErrorLogger.LogInfo("Database initialized");

        // Initialize blocking service (loads rules) - AWAIT to ensure it completes
        var blockingService = _serviceProvider.GetRequiredService<IBlockingService>();
        await blockingService.InitializeAsync();

        ErrorLogger.LogInfo("Blocking service initialized");

        // Start network logger background task - AWAIT to ensure it starts
        var networkLogger = _serviceProvider.GetRequiredService<INetworkLogger>();
        await networkLogger.StartAsync();

        ErrorLogger.LogInfo("Network logger started");

        // Initialize search engine from saved settings
        var settingsService = _serviceProvider.GetRequiredService<SettingsService>();
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

        ErrorLogger.LogInfo("Channel sync service initialized (manual sync only)");

        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        ErrorLogger.LogInfo("Main window shown - startup complete");
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

        // Phase 3: Rule System Services
        services.AddSingleton<IRuleEngine, RuleEngine>();
        services.AddSingleton<IBlockingService, BlockingService>();
        services.AddSingleton<CSSInjector>();
        services.AddSingleton<ICSSInjector>(sp => sp.GetRequiredService<CSSInjector>());
        services.AddSingleton<JSInjector>();
        services.AddSingleton<IJSInjector>(sp => sp.GetRequiredService<JSInjector>());

        // Network Monitoring Services (Phase 2) - with blocking support
        services.AddSingleton<RequestInterceptor>(sp => new RequestInterceptor(
            sp.GetRequiredService<IBlockingService>()));
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

        // Settings Service
        services.AddSingleton<SettingsService>();

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
        services.AddSingleton<BookmarksPanel>();

        // Phase 6: AI Views
        services.AddSingleton<CopilotSidebarView>();

        // Phase 9: New Views
        services.AddSingleton<DownloadManagerView>();
        services.AddTransient<ExtensionManagerView>();
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
