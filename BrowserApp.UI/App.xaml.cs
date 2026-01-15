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

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Ensure database is created
        EnsureDatabase();

        // Start network logger background task
        var networkLogger = _serviceProvider.GetRequiredService<INetworkLogger>();
        _ = networkLogger.StartAsync();

        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
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

        // UI Services (Singleton - shared state for WebView2)
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

        // Network Monitoring Services (Phase 2)
        services.AddSingleton<RequestInterceptor>();
        services.AddSingleton<IRequestInterceptor>(sp => sp.GetRequiredService<RequestInterceptor>());
        services.AddSingleton<INetworkLogger, NetworkLogger>();

        // Data Services
        services.AddDbContext<BrowserDbContext>(options =>
        {
            string dbPath = BrowserDbContext.GetDatabasePath();
            options.UseSqlite($"Data Source={dbPath}");
        });
        services.AddScoped<IBrowsingHistoryRepository, BrowsingHistoryRepository>();
        services.AddScoped<INetworkLogRepository, NetworkLogRepository>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<NetworkMonitorViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
        services.AddSingleton<NetworkMonitorView>();
    }

    private void EnsureDatabase()
    {
        using var scope = _serviceProvider!.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BrowserDbContext>();
        dbContext.Database.EnsureCreated();
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
