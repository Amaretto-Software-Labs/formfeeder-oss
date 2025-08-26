using FormFeeder.Api.Models;
using Microsoft.Extensions.Options;

namespace FormFeeder.Api.Services;

public sealed class FormConfigurationService : IFormConfigurationService
{
    private readonly List<FormConfiguration> _formConfigurations;
    private readonly ILogger<FormConfigurationService> _logger;

    public FormConfigurationService(IConfiguration configuration, ILogger<FormConfigurationService> logger)
    {
        _logger = logger;
        
        // Load from configuration for now, can be replaced with DB later
        _formConfigurations = configuration.GetSection("Forms").Get<List<FormConfiguration>>() ?? [];
        
        // Validate privacy mode configuration for all forms
        ValidatePrivacyModeConfigurations();
        
        _logger.LogInformation("Loaded {Count} form configurations", _formConfigurations.Count);
    }

    public Task<FormConfiguration?> GetFormConfigurationAsync(string formId)
    {
        var config = _formConfigurations.FirstOrDefault(f => 
            string.Equals(f.FormId, formId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(config);
    }

    public Task<IEnumerable<FormConfiguration>> GetAllFormConfigurationsAsync()
    {
        return Task.FromResult<IEnumerable<FormConfiguration>>(_formConfigurations);
    }

    public async Task<bool> IsFormEnabledAsync(string formId)
    {
        var config = await GetFormConfigurationAsync(formId);
        return config?.Enabled ?? false;
    }

    public async Task<bool> IsDomainAllowedForFormAsync(string formId, string? domain)
    {
        if (string.IsNullOrEmpty(domain))
            return false;

        var config = await GetFormConfigurationAsync(formId);
        if (config == null || !config.Enabled)
            return false;

        // If AllowedDomains contains "*", allow all domains
        if (config.AllowedDomains.Contains("*"))
            return true;

        // Check if the domain matches any allowed domain (exact match or subdomain)
        return config.AllowedDomains.Any(allowedDomain => 
            domain.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase) ||
            domain.EndsWith($".{allowedDomain}", StringComparison.OrdinalIgnoreCase));
    }

    public Task<HashSet<string>> GetAllAllowedDomainsAsync()
    {
        var allDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var config in _formConfigurations.Where(f => f.Enabled))
        {
            foreach (var domain in config.AllowedDomains)
            {
                if (domain != "*")
                {
                    allDomains.Add(domain);
                }
            }
        }

        return Task.FromResult(allDomains);
    }

    private void ValidatePrivacyModeConfigurations()
    {
        var invalidConfigurations = _formConfigurations
            .Where(config => !config.IsPrivacyModeValid())
            .ToList();

        foreach (var invalidConfig in invalidConfigurations)
        {
            _logger.LogWarning(
                "Privacy mode is enabled for form '{FormId}' but no connectors are enabled. " +
                "Privacy mode requires at least one enabled connector to be meaningful. " +
                "Form submissions will be rejected until this is corrected.", 
                invalidConfig.FormId);
        }

        if (invalidConfigurations.Count > 0)
        {
            _logger.LogWarning("Found {Count} invalid privacy mode configurations that need attention", 
                invalidConfigurations.Count);
        }
    }
}