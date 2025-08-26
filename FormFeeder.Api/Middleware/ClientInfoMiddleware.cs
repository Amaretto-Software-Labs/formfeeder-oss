using FormFeeder.Api.Utilities;

namespace FormFeeder.Api.Middleware;

public sealed class ClientInfoMiddleware(
    RequestDelegate next,
    ILogger<ClientInfoMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var queryString = context.Request.QueryString;
        var fullPath = path + queryString;
        var method = context.Request.Method;
        var clientIp = context.Request.GetClientIpAddress();
        var userAgent = context.Request.Headers.UserAgent.ToString();

        logger.LogInformation("Request: {Method} {Path} from {ClientIp} - UserAgent: {UserAgent}", 
            method, fullPath, clientIp, userAgent);

        await next(context).ConfigureAwait(false);
    }

}