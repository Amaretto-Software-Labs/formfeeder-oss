using FormFeeder.Api.Connectors;

namespace FormFeeder.Api.Models;

public sealed class FormConfiguration
{
    public required string FormId { get; set; }
    public List<string> AllowedDomains { get; set; } = [];
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;
    public RateLimitSettings? RateLimit { get; set; }
    public List<ConnectorConfiguration>? Connectors { get; set; }
    public bool PrivacyMode { get; set; } = false;
    
    public bool HasEnabledConnectors() =>
        Connectors?.Any(c => c.Enabled) ?? false;
    
    public bool IsPrivacyModeValid() =>
        !PrivacyMode || HasEnabledConnectors();
}

public sealed class RateLimitSettings
{
    public int RequestsPerWindow { get; set; } = 10;
    public int WindowMinutes { get; set; } = 1;
}