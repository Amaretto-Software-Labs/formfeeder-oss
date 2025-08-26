using FormFeeder.Api.Models;
using FormFeeder.Api.Services.Configuration;

using Microsoft.Extensions.Options;

namespace FormFeeder.Api.Services;

public sealed class FormConfigurationSeedingService
{
    private readonly IConfiguration configuration;
    private readonly IFormConfigurationManagementService managementService;
    private readonly IFormConfigurationService readOnlyService;
    private readonly FormConfigurationProviderSettings providerSettings;
    private readonly ILogger<FormConfigurationSeedingService> logger;

    public FormConfigurationSeedingService(
        IConfiguration configuration,
        IFormConfigurationManagementService managementService,
        IFormConfigurationService readOnlyService,
        IOptions<FormConfigurationProviderSettings> providerSettings,
        ILogger<FormConfigurationSeedingService> logger)
    {
        this.configuration = configuration;
        this.managementService = managementService;
        this.readOnlyService = readOnlyService;
        this.providerSettings = providerSettings.Value;
        this.logger = logger;
    }

    public async Task SeedFromAppSettingsAsync()
    {
        if (!providerSettings.SeedFromAppSettings)
        {
            logger.LogDebug("Seeding from appsettings is disabled");
            return;
        }

        try
        {
            // Load configurations from appsettings
            var appSettingsConfigs = configuration.GetSection("Forms").Get<List<FormConfiguration>>() ?? [];

            if (appSettingsConfigs.Count == 0)
            {
                logger.LogInformation("No form configurations found in appsettings to seed");
                return;
            }

            var existingConfigs = (await readOnlyService.GetAllFormConfigurationsAsync()).ToList();
            var existingFormIds = existingConfigs.Select(c => c.FormId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var configsToSeed = new List<FormConfiguration>();
            var configsToUpdate = new List<FormConfiguration>();

            foreach (var config in appSettingsConfigs)
            {
                if (existingFormIds.Contains(config.FormId))
                {
                    if (providerSettings.OverwriteExistingOnSeed)
                    {
                        configsToUpdate.Add(config);
                    }
                    else
                    {
                        logger.LogDebug("Skipping existing form configuration: {FormId}", config.FormId);
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
                    await managementService.CreateFormConfigurationAsync(config);
                    logger.LogInformation("Seeded form configuration: {FormId}", config.FormId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to seed form configuration: {FormId}", config.FormId);
                }
            }

            // Update existing configurations if overwrite is enabled
            foreach (var config in configsToUpdate)
            {
                try
                {
                    await managementService.UpdateFormConfigurationAsync(config);
                    logger.LogInformation("Updated existing form configuration during seeding: {FormId}", config.FormId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to update form configuration during seeding: {FormId}", config.FormId);
                }
            }

            logger.LogInformation(
                "Form configuration seeding completed. Created: {Created}, Updated: {Updated}, Skipped: {Skipped}",
                configsToSeed.Count,
                configsToUpdate.Count,
                existingConfigs.Count - configsToUpdate.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during form configuration seeding");
            throw;
        }
    }
}
