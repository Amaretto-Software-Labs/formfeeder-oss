using FormFeeder.Api.Data;
using FormFeeder.Api.Models;
using FormFeeder.Api.Models.Entities;
using FormFeeder.Api.Services.Extensions;

using Microsoft.EntityFrameworkCore;

namespace FormFeeder.Api.Services;

public sealed class DatabaseFormConfigurationService : IFormConfigurationManagementService
{
    private readonly AppDbContext context;
    private readonly ILogger<DatabaseFormConfigurationService> logger;

    public DatabaseFormConfigurationService(AppDbContext context, ILogger<DatabaseFormConfigurationService> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task<FormConfiguration?> GetFormConfigurationAsync(string formId)
    {
        if (string.IsNullOrWhiteSpace(formId))
        {
            return null;
        }

        try
        {
            var entity = await context.FormConfigurations
                .Include(f => f.AllowedDomains)
                .Include(f => f.RateLimit)
                .Include(f => f.Connectors)
                .FirstOrDefaultAsync(f => f.FormId.ToLower() == formId.ToLower());

            return entity?.ToFormConfiguration();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving form configuration for FormId: {FormId}", formId);
            return null;
        }
    }

    public async Task<IEnumerable<FormConfiguration>> GetAllFormConfigurationsAsync()
    {
        try
        {
            var entities = await context.FormConfigurations
                .Include(f => f.AllowedDomains)
                .Include(f => f.RateLimit)
                .Include(f => f.Connectors)
                .ToListAsync();

            return entities.Select(e => e.ToFormConfiguration());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all form configurations");
            return [];
        }
    }

    public async Task<bool> IsFormEnabledAsync(string formId)
    {
        if (string.IsNullOrWhiteSpace(formId))
        {
            return false;
        }

        try
        {
            return await context.FormConfigurations
                .Where(f => f.FormId.ToLower() == formId.ToLower())
                .Select(f => f.Enabled)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if form is enabled for FormId: {FormId}", formId);
            return false;
        }
    }

    public async Task<bool> IsDomainAllowedForFormAsync(string formId, string? domain)
    {
        if (string.IsNullOrWhiteSpace(formId) || string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        try
        {
            var form = await context.FormConfigurations
                .Include(f => f.AllowedDomains)
                .Where(f => f.FormId.ToLower() == formId.ToLower() && f.Enabled)
                .FirstOrDefaultAsync();

            if (form == null)
            {
                return false;
            }

            var allowedDomains = form.AllowedDomains.Select(d => d.Domain).ToList();

            // If AllowedDomains contains "*", allow all domains
            if (allowedDomains.Contains("*"))
            {
                return true;
            }

            // Check if the domain matches any allowed domain (exact match or subdomain)
            return allowedDomains.Any(allowedDomain =>
                domain.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase) ||
                domain.EndsWith($".{allowedDomain}", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking domain allowance for FormId: {FormId}, Domain: {Domain}", formId, domain);
            return false;
        }
    }

    public async Task<HashSet<string>> GetAllAllowedDomainsAsync()
    {
        try
        {
            var domains = await context.AllowedDomains
                .Where(d => d.FormConfiguration.Enabled && d.Domain != "*")
                .Select(d => d.Domain)
                .Distinct()
                .ToListAsync();

            return new HashSet<string>(domains, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all allowed domains");
            return [];
        }
    }

    public async Task<FormConfiguration> CreateFormConfigurationAsync(FormConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            var entity = config.ToFormConfigurationEntity();

            // Validate privacy mode configuration
            var mappedConfig = entity.ToFormConfiguration();
            if (!mappedConfig.IsPrivacyModeValid())
            {
                logger.LogWarning(
                    "Creating form configuration '{FormId}' with invalid privacy mode configuration. " +
                    "Privacy mode is enabled but no connectors are enabled.",
                    config.FormId);
            }

            context.FormConfigurations.Add(entity);
            await context.SaveChangesAsync();

            logger.LogInformation("Created form configuration for FormId: {FormId}", config.FormId);

            // Return the entity with populated navigation properties
            return await GetFormConfigurationAsync(config.FormId) ?? config;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating form configuration for FormId: {FormId}", config.FormId);
            throw;
        }
    }

    public async Task<FormConfiguration?> UpdateFormConfigurationAsync(FormConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            var existingEntity = await context.FormConfigurations
                .Include(f => f.AllowedDomains)
                .Include(f => f.RateLimit)
                .Include(f => f.Connectors)
                .FirstOrDefaultAsync(f => f.FormId.ToLower() == config.FormId.ToLower());

            if (existingEntity == null)
            {
                return null;
            }

            // Update scalar properties
            existingEntity.Description = config.Description;
            existingEntity.Enabled = config.Enabled;
            existingEntity.PrivacyMode = config.PrivacyMode;
            existingEntity.UpdatedAt = DateTime.UtcNow;

            // Update allowed domains (remove existing, add new ones)
            var existingDomains = existingEntity.AllowedDomains.ToList();
            foreach (var domain in existingDomains)
            {
                context.AllowedDomains.Remove(domain);
            }

            existingEntity.AllowedDomains.Clear();

            foreach (var domain in config.AllowedDomains)
            {
                context.AllowedDomains.Add(new AllowedDomainEntity
                {
                    Domain = domain,
                    FormConfigurationId = existingEntity.Id,
                });
            }

            // Update rate limit settings
            if (existingEntity.RateLimit != null)
            {
                context.RateLimitSettings.Remove(existingEntity.RateLimit);
                existingEntity.RateLimit = null;
            }

            if (config.RateLimit != null)
            {
                var rateLimitEntity = new RateLimitSettingsEntity
                {
                    RequestsPerWindow = config.RateLimit.RequestsPerWindow,
                    WindowMinutes = config.RateLimit.WindowMinutes,
                    FormConfigurationId = existingEntity.Id,
                };
                context.RateLimitSettings.Add(rateLimitEntity);
                existingEntity.RateLimit = rateLimitEntity;
            }

            // Update connector configurations (remove existing, add new ones)
            var existingConnectors = existingEntity.Connectors.ToList();
            foreach (var connector in existingConnectors)
            {
                context.ConnectorConfigurations.Remove(connector);
            }

            existingEntity.Connectors.Clear();

            if (config.Connectors != null && config.Connectors.Count > 0)
            {
                foreach (var connectorConfig in config.Connectors)
                {
                    var connectorEntity = connectorConfig.ToConnectorConfigurationEntity();
                    connectorEntity.FormConfigurationId = existingEntity.Id;
                    context.ConnectorConfigurations.Add(connectorEntity);
                    existingEntity.Connectors.Add(connectorEntity);
                }
            }

            // Validate privacy mode configuration
            var mappedConfig = existingEntity.ToFormConfiguration();
            if (!mappedConfig.IsPrivacyModeValid())
            {
                logger.LogWarning(
                    "Updating form configuration '{FormId}' with invalid privacy mode configuration. " +
                    "Privacy mode is enabled but no connectors are enabled.",
                    config.FormId);
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Updated form configuration for FormId: {FormId}", config.FormId);

            return existingEntity.ToFormConfiguration();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating form configuration for FormId: {FormId}", config.FormId);
            throw;
        }
    }

    public async Task<bool> DeleteFormConfigurationAsync(string formId)
    {
        if (string.IsNullOrWhiteSpace(formId))
        {
            return false;
        }

        try
        {
            var entity = await context.FormConfigurations
                .FirstOrDefaultAsync(f => f.FormId.ToLower() == formId.ToLower());

            if (entity == null)
            {
                return false;
            }

            context.FormConfigurations.Remove(entity);
            await context.SaveChangesAsync();

            logger.LogInformation("Deleted form configuration for FormId: {FormId}", formId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting form configuration for FormId: {FormId}", formId);
            throw;
        }
    }
}
