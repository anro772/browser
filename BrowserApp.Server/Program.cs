using Microsoft.EntityFrameworkCore;
using BrowserApp.Server.Data;
using BrowserApp.Server.Data.Repositories;
using BrowserApp.Server.Interfaces;
using BrowserApp.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Database
builder.Services.AddDbContext<ServerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMarketplaceRuleRepository, MarketplaceRuleRepository>();

// Services
builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();

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

// CORS - allow client connections
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Apply migrations on startup
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
