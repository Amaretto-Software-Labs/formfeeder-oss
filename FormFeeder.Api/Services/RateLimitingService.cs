using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using FormFeeder.Api.Models;
using FormFeeder.Api.Utilities;

namespace FormFeeder.Api.Services;

public static class RateLimitingService
{
    public const string GlobalPolicy = "global";
    public const string PerFormPolicy = "per-form";
    
    public static IServiceCollection AddFormRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            // Global rate limit - applies to all requests
            options.AddFixedWindowLimiter(GlobalPolicy, limiterOptions =>
            {
                limiterOptions.PermitLimit = configuration.GetValue<int>("RateLimiting:Global:PermitLimit", 1000);
                limiterOptions.Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:Global:WindowMinutes", 1));
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = configuration.GetValue<int>("RateLimiting:Global:QueueLimit", 10);
            });
            
            // Per-form rate limit - based on form ID and IP
            options.AddPolicy(PerFormPolicy, context =>
            {
                // Extract form ID from the route
                var formId = context.Request.RouteValues["formId"]?.ToString() ?? "unknown";
                
                // Get client IP
                var clientIp = context.Request.GetClientIpAddress() ?? "unknown";
                
                // Create a unique key for this form and IP combination
                var partitionKey = $"form_{formId}_{clientIp}";
                
                // Get form configuration service to read rate limits
                using var scope = context.RequestServices.CreateScope();
                var formConfigService = scope.ServiceProvider.GetService<IFormConfigurationService>();
                var formConfig = formConfigService?.GetFormConfigurationAsync(formId).GetAwaiter().GetResult();
                
                // Use form-specific rate limits if available, otherwise use defaults
                var permitLimit = formConfig?.RateLimit?.RequestsPerWindow 
                    ?? configuration.GetValue<int>("RateLimiting:PerForm:DefaultPermitLimit", 10);
                var windowMinutes = formConfig?.RateLimit?.WindowMinutes 
                    ?? configuration.GetValue<int>("RateLimiting:PerForm:DefaultWindowMinutes", 1);
                
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    key => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(windowMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    });
            });
            
            // Custom response for rate limit exceeded
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                
                var response = new
                {
                    error = "Too many requests",
                    message = "You have exceeded the rate limit for this form. Please try again later.",
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) 
                        ? retryAfter.TotalSeconds 
                        : 60
                };
                
                await context.HttpContext.Response.WriteAsJsonAsync(response, token);
            };
        });
        
        return services;
    }
    
}