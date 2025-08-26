using FormFeeder.Api.Services;
using FormFeeder.Api.Services.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FormFeeder.Api.Data;

public static class DatabaseBootstrap
{
    public static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<AppDbContext>();

            // Check if we can connect to the database
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogWarning("Cannot connect to database. Skipping migrations.");
                return;
            }

            // Get pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                logger.LogInformation("Database is up to date, no migrations needed");
            }

            // Seed form configurations if using database provider
            await SeedFormConfigurationsAsync(services, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");

            // In production, you might want to fail fast
            if (app.Environment.IsProduction())
            {
                throw new Exception("Database initialization failed", ex);
            }
        }
    }

    private static async Task SeedFormConfigurationsAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            var providerSettings = services.GetRequiredService<IOptions<FormConfigurationProviderSettings>>().Value;

            // Only seed if using database provider
            if (providerSettings.Type != FormConfigurationProviderType.Database)
            {
                logger.LogDebug("Skipping form configuration seeding - not using database provider");
                return;
            }

            var seedingService = services.GetService<FormConfigurationSeedingService>();
            if (seedingService != null)
            {
                await seedingService.SeedFromAppSettingsAsync();
            }
            else
            {
                logger.LogWarning("FormConfigurationSeedingService not available for seeding");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during form configuration seeding");

            // Don't rethrow - seeding is optional
        }
    }
}
