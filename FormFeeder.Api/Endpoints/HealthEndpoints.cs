using FormFeeder.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FormFeeder.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (AppDbContext context) =>
        {
            try
            {
                // Check database connectivity
                var canConnect = await context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    return Results.Ok(new
                    {
                        status = "Healthy",
                        database = "Connected",
                        timestamp = DateTime.UtcNow
                    });
                }
                
                return Results.Json(new
                {
                    status = "Degraded",
                    database = "Not Connected",
                    timestamp = DateTime.UtcNow
                }, statusCode: 503);
            }
            catch (Exception ex)
            {
                return Results.Json(new
                {
                    status = "Unhealthy",
                    database = "Error",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                }, statusCode: 503);
            }
        })
        .WithName("HealthCheck")
        .WithOpenApi()
        .Produces(200)
        .Produces(503);
        
        app.MapGet("/health/ready", async (AppDbContext context) =>
        {
            try
            {
                // More thorough readiness check
                await context.Database.ExecuteSqlRawAsync("SELECT 1");
                
                // Check if migrations are applied
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                var hasPendingMigrations = pendingMigrations.Any();
                
                if (!hasPendingMigrations)
                {
                    return Results.Ok(new
                    {
                        ready = true,
                        database = "Ready",
                        migrations = "Applied",
                        timestamp = DateTime.UtcNow
                    });
                }
                
                return Results.Json(new
                {
                    ready = false,
                    database = "Connected",
                    migrations = $"{pendingMigrations.Count()} pending",
                    timestamp = DateTime.UtcNow
                }, statusCode: 503);
            }
            catch (Exception ex)
            {
                return Results.Json(new
                {
                    ready = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                }, statusCode: 503);
            }
        })
        .WithName("ReadinessCheck")
        .WithOpenApi()
        .Produces(200)
        .Produces(503);
    }
}