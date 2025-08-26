namespace FormFeeder.Api.Utilities;

public static class HttpRequestExtensions
{
    public static string? GetClientIpAddress(this HttpRequest request)
    {
        var forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                var trimmedIp = ips[0].Trim();
                if (!string.IsNullOrWhiteSpace(trimmedIp))
                {
                    return trimmedIp;
                }
            }
        }

        var realIp = request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp;
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}