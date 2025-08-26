using FormFeeder.Api.Data;
using FormFeeder.Api.Services;
using FormFeeder.Api.Services.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FormFeeder.Api.Tests.Infrastructure;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Use a connection string that will fail but parse correctly
                ["ConnectionStrings:DefaultConnection"] = "Host=nonexistent;Database=test;Username=test;Password=test;",
                ["FormConfigurationProvider:Type"] = "Database",
                ["EnableSwagger"] = "false",
                ["Forms:0:FormId"] = "test-form",
                ["Forms:0:Enabled"] = "true",
                ["Forms:0:AllowedDomains:0"] = "example.com",
                ["MailJetDefaults:ApiKey"] = "bd810bf147bc374e556a7e1d7c544a3b",
                ["MailJetDefaults:ApiSecret"] = "9af927ad95719b42dcae904905bb9b6c",
                ["MailJetDefaults:FromEmail"] = "no-reply@vasoftware.co.uk",
                ["MailJetDefaults:FromName"] = "FormFeeder",
                ["MailJetDefaults:Subject"] = "New Form Submission",
                ["MailJetDefaults:TemplateId"] = "7251954",
                ["AllowedOrigins:0"] = "https://example.com",
                ["AllowedOrigins:1"] = "http://localhost:3000",
                ["AllowedOrigins:2"] = "https://subdomain.example.com"
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            // Remove all Entity Framework related services to avoid conflicts
            RemoveEntityFrameworkServices(services);

            // Create separate service provider for EF to avoid conflicts
            var efServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Add Entity Framework InMemory Database for testing with internal service provider
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
                options.UseInternalServiceProvider(efServiceProvider);
                options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            // Register services that the main Program.cs registers but might be missing in test setup
            var configuration = context.Configuration;
            
            // Ensure we have all the required services
            EnsureRequiredServices(services, configuration);
        });
    }

    private static void RemoveEntityFrameworkServices(IServiceCollection services)
    {
        // Remove all EF-related services to avoid database provider conflicts
        var serviceTypesToRemove = new[]
        {
            typeof(DbContextOptions<AppDbContext>),
            typeof(DbContextOptions),
            typeof(AppDbContext)
        };

        foreach (var serviceType in serviceTypesToRemove)
        {
            var descriptorsToRemove = services.Where(d => d.ServiceType == serviceType).ToList();
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }
        }
    }

    private static void EnsureRequiredServices(IServiceCollection services, IConfiguration configuration)
    {
        // Only add services if they're not already registered
        if (!services.Any(s => s.ServiceType == typeof(HttpClient)))
            services.AddHttpClient();

        if (!services.Any(s => s.ServiceType == typeof(IFormConfigurationService)))
            services.AddFormConfiguration(configuration);

        if (!services.Any(s => s.ServiceType == typeof(IPrivateFormGenerationService)))
            services.AddPrivateFormGeneration(configuration);

        if (!services.Any(s => s.ServiceType == typeof(FormSubmissionService)))
            services.AddScoped<FormSubmissionService>();

        if (!services.Any(s => s.ServiceType == typeof(FormConfigurationSeedingService)))
            services.AddScoped<FormConfigurationSeedingService>();

        // SOLID principle services - only add if missing
        if (!services.Any(s => s.ServiceType == typeof(IFormValidationService)))
            services.AddScoped<IFormValidationService, FormValidationService>();

        if (!services.Any(s => s.ServiceType == typeof(IFormDataExtractionService)))
            services.AddScoped<IFormDataExtractionService, FormDataExtractionService>();

        if (!services.Any(s => s.ServiceType == typeof(IEmailTemplateService)))
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        if (!services.Any(s => s.ServiceType == typeof(IConnectorFactory)))
            services.AddScoped<IConnectorFactory, ConnectorFactory>();

        if (!services.Any(s => s.ServiceType == typeof(IConnectorService)))
            services.AddScoped<IConnectorService, ConnectorService>();
    }
}