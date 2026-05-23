using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using BrowserApp.Server.Data;
using BrowserApp.Server.Data.Repositories;
using BrowserApp.Server.Interfaces;
using BrowserApp.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Validate connection string
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("PostgreSQL connection string is not configured. Please set ConnectionStrings:PostgreSQL in appsettings.json");
}

// Database
builder.Services.AddDbContext<ServerDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMarketplaceRuleRepository, MarketplaceRuleRepository>();
builder.Services.AddScoped<IChannelRepository, ChannelRepository>();

// Services
builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();
builder.Services.AddScoped<IChannelService, ChannelService>();

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "BrowserApp Marketplace API",
        Version = "v1",
        Description = "API for the BrowserApp rules marketplace"
    });
});

// CORS - Development only allows any origin, Production requires specific origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // TODO: Configure production origins in appsettings.json
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "https://yourdomain.com" };
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Fly.io (and most cloud hosts) terminate TLS at the edge and forward to the
// container as plain HTTP, setting X-Forwarded-Proto: https on the way in.
// Without honoring those headers, ASP.NET builds Location/redirect URLs as
// http://... which causes downgrade-detection failures in HTTPS-strict clients
// (e.g. PowerShell's Invoke-RestMethod refusing to follow a 201 Created
// Location header on POSTed resources).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Clear the default loopback-only restriction — Fly's proxy IP isn't loopback.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Must run before any middleware that emits URLs (CORS, controllers, Swagger).
app.UseForwardedHeaders();

// Configure the HTTP request pipeline. Swagger is also enabled in Production so
// the deployed API is browsable at /swagger for diagnostics.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowClient");

app.UseAuthorization();

app.MapControllers();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations");
        throw;
    }
}

app.Run();
