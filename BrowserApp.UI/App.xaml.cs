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

    protected override void OnStartup(StartupEventArgs e)
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
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            ErrorLogger.LogInfo("Services configured");

            // Ensure database is created
            EnsureDatabase();

            ErrorLogger.LogInfo("Database initialized");

            // Initialize blocking service (loads rules)
            var blockingService = _serviceProvider.GetRequiredService<IBlockingService>();
            _ = blockingService.InitializeAsync();

            ErrorLogger.LogInfo("Blocking service initialized");

            // Start network logger background task
            var networkLogger = _serviceProvider.GetRequiredService<INetworkLogger>();
            _ = networkLogger.StartAsync();

            ErrorLogger.LogInfo("Network logger started");

            // Show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            ErrorLogger.LogInfo("Main window shown - startup complete");
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

    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        services.AddSingleton<IConfiguration>(config);

        // Core Services
        services.AddTransient<ISearchEngineService, SearchEngineService>();

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

        // Navigation Service with injection support
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

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<NetworkMonitorViewModel>();
        services.AddTransient<RuleManagerViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
        services.AddSingleton<NetworkMonitorView>();
        services.AddTransient<RuleManagerView>();
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
            // Log the error and try to recover
            ErrorLogger.LogError("Database migration failed", ex);

            try
            {
                ErrorLogger.LogInfo("Attempting to delete and recreate database");
                // Database exists but was created without migrations (legacy)
                // Delete and recreate with proper migrations
                dbContext.Database.EnsureDeleted();
                dbContext.Database.Migrate();
                ErrorLogger.LogInfo("Database recreated successfully");
            }
            catch (Exception innerEx)
            {
                ErrorLogger.LogError("Database recreation failed", innerEx);
                throw;
            }
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // Stop network logger gracefully
        if (_serviceProvider != null)
        {
            var networkLogger = _serviceProvider.GetService<INetworkLogger>();
            if (networkLogger != null)
            {
                await networkLogger.DisposeAsync();
            }
        }

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}
