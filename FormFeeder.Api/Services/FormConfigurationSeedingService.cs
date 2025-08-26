using FormFeeder.Api.Models;
using FormFeeder.Api.Services.Configuration;
using Microsoft.Extensions.Options;

namespace FormFeeder.Api.Services;

public sealed class FormConfigurationSeedingService
{
    private readonly IConfiguration _configuration;
    private readonly IFormConfigurationManagementService _managementService;
    private readonly IFormConfigurationService _readOnlyService;
    private readonly FormConfigurationProviderSettings _providerSettings;
    private readonly ILogger<FormConfigurationSeedingService> _logger;

    public FormConfigurationSeedingService(
        IConfiguration configuration,
        IFormConfigurationManagementService managementService,
        IFormConfigurationService readOnlyService,
        IOptions<FormConfigurationProviderSettings> providerSettings,
        ILogger<FormConfigurationSeedingService> logger)
    {
        _configuration = configuration;
        _managementService = managementService;
        _readOnlyService = readOnlyService;
        _providerSettings = providerSettings.Value;
        _logger = logger;
    }

    public async Task SeedFromAppSettingsAsync()
    {
        if (!_providerSettings.SeedFromAppSettings)
        {
            _logger.LogDebug("Seeding from appsettings is disabled");
            return;
        }

        try
        {
            // Load configurations from appsettings
            var appSettingsConfigs = _configuration.GetSection("Forms").Get<List<FormConfiguration>>() ?? [];
            
            if (appSettingsConfigs.Count == 0)
            {
                _logger.LogInformation("No form configurations found in appsettings to seed");
                return;
            }

            var existingConfigs = (await _readOnlyService.GetAllFormConfigurationsAsync()).ToList();
            var existingFormIds = existingConfigs.Select(c => c.FormId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var configsToSeed = new List<FormConfiguration>();
            var configsToUpdate = new List<FormConfiguration>();

            foreach (var config in appSettingsConfigs)
            {
                if (existingFormIds.Contains(config.FormId))
                {
                    if (_providerSettings.OverwriteExistingOnSeed)
                    {
                        configsToUpdate.Add(config);
                    }
                    else
                    {
                        _logger.LogDebug("Skipping existing form configuration: {FormId}", config.FormId);
                    }
                }
                else
                {
                    configsToSeed.Add(config);
                }
            }

            // Create new configurations
            foreach (var config in configsToSeed)
            {
                try
                {
                    await _managementService.CreateFormConfigurationAsync(config);
                    _logger.LogInformation("Seeded form configuration: {FormId}", config.FormId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to seed form configuration: {FormId}", config.FormId);
                }
            }

            // Update existing configurations if overwrite is enabled
            foreach (var config in configsToUpdate)
            {
                try
                {
                    await _managementService.UpdateFormConfigurationAsync(config);
                    _logger.LogInformation("Updated existing form configuration during seeding: {FormId}", config.FormId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update form configuration during seeding: {FormId}", config.FormId);
                }
            }

            _logger.LogInformation(
                "Form configuration seeding completed. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}",
                configsToSeed.Count,
                configsToUpdate.Count,
                existingConfigs.Count - configsToUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during form configuration seeding");
            throw;
        }
    }
}