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

        // Data Services
        services.AddDbContext<BrowserDbContext>(options =>
        {
            string dbPath = BrowserDbContext.GetDatabasePath();
            options.UseSqlite($"Data Source={dbPath}");
        });
        services.AddScoped<IBrowsingHistoryRepository, BrowsingHistoryRepository>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    private void EnsureDatabase()
    {
        using var scope = _serviceProvider!.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BrowserDbContext>();
        dbContext.Database.EnsureCreated();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}
