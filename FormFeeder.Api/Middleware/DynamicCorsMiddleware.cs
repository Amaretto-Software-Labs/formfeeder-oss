using FormFeeder.Api.Services;

namespace FormFeeder.Api.Middleware;

public sealed class DynamicCorsMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        
        if (!string.IsNullOrEmpty(origin))
        {
            var isAllowed = false;
            
            // Check if this is a form submission endpoint (e.g., /v1/form/{formId})
            if (context.Request.Path.StartsWithSegments("/v1/form") && 
                context.Request.Path.Value?.StartsWith("/v1/form/") == true)
            {
                // Extract form ID from path
                var pathSegments = context.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (pathSegments.Length >= 3)
                {
                    var formId = pathSegments[2];
                    
                    // Create a scope to resolve scoped services
                    using var scope = context.RequestServices.CreateScope();
                    var formConfigService = scope.ServiceProvider.GetRequiredService<IFormConfigurationService>();
                    
                    // Get form configuration to check its allowed domains
                    var formConfig = await formConfigService.GetFormConfigurationAsync(formId).ConfigureAwait(false);
                    if (formConfig != null && formConfig.Enabled)
                    {
                        isAllowed = formConfig.AllowedDomains.Contains("*") || formConfig.AllowedDomains.Contains(origin);
                    }
                }
            }
            else
            {
                // For all other endpoints, use the generic AllowedOrigins from configuration
                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<List<string>>() ?? [];
                isAllowed = allowedOrigins.Contains("*") || allowedOrigins.Contains(origin);
            }
            
            if (isAllowed)
            {
                context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
                context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
                context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
            }
        }
        
        // Handle preflight requests
        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 204;
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}