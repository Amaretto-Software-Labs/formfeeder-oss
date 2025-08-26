using FormFeeder.Api.Services.Configuration;
using Microsoft.Extensions.Options;

namespace FormFeeder.Api.Services.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds form configuration services based on the configured provider type.
    /// Follows Single Responsibility and Dependency Inversion principles.
    /// </summary>
    public static IServiceCollection AddFormConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure the provider settings
        services.Configure<FormConfigurationProviderSettings>(
            configuration.GetSection(FormConfigurationProviderSettings.SectionName));

        // Register the appropriate service implementation as scoped
        services.AddScoped<IFormConfigurationService>(serviceProvider =>
        {
            var providerSettings = serviceProvider.GetRequiredService<IOptions<FormConfigurationProviderSettings>>().Value;

            return providerSettings.Type switch
            {
                FormConfigurationProviderType.Database => 
                    serviceProvider.GetRequiredService<DatabaseFormConfigurationService>(),
                FormConfigurationProviderType.AppSettings or _ => 
                    serviceProvider.GetRequiredService<FormConfigurationService>()
            };
        });

        // Register FormConfigurationService as singleton (no DB dependency)
        services.AddSingleton<FormConfigurationService>();
        // Register DatabaseFormConfigurationService as scoped (has DB dependency)
        services.AddScoped<DatabaseFormConfigurationService>();
        
        // Register the management service as scoped (only used in endpoints and seeding)
        services.AddScoped<IFormConfigurationManagementService>(serviceProvider =>
        {
            var providerSettings = serviceProvider.GetRequiredService<IOptions<FormConfigurationProviderSettings>>().Value;
            
            return providerSettings.Type switch
            {
                FormConfigurationProviderType.Database => 
                    serviceProvider.GetRequiredService<DatabaseFormConfigurationService>(),
                _ => throw new InvalidOperationException(
                    $"Management operations are only supported for Database provider. Current provider: {providerSettings.Type}")
            };
        });

        return services;
    }

    /// <summary>
    /// Adds private form generation services for auto-creating forms from email addresses.
    /// </summary>
    public static IServiceCollection AddPrivateFormGeneration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure MailJet defaults
        services.Configure<MailJetDefaults>(
            configuration.GetSection(MailJetDefaults.ConfigurationSectionName));

        // Register private form generation services
        services.AddScoped<IFormIdGenerationService, FormIdGenerationService>();
        services.AddScoped<IPrivateFormGenerationService, PrivateFormGenerationService>();

        return services;
    }
}