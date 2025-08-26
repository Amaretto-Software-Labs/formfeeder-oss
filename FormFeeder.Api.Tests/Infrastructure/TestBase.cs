using FormFeeder.Api.Data;
using FormFeeder.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FormFeeder.Api.Tests.Infrastructure;

public abstract class TestBase : IDisposable
{
    protected readonly ServiceProvider ServiceProvider;
    protected readonly AppDbContext DbContext;

    protected TestBase()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=FormFeederTest;",
                ["RateLimiting:Global:PermitLimit"] = "1000",
                ["RateLimiting:Global:WindowMinutes"] = "1",
                ["RateLimiting:Global:QueueLimit"] = "10",
                ["RateLimiting:PerForm:DefaultPermitLimit"] = "10",
                ["RateLimiting:PerForm:DefaultWindowMinutes"] = "1"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add DbContext using AddDbContext with proper in-memory configuration
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        });
        
        // Register core service interfaces with mocks by default
        services.AddScoped<IFormConfigurationService>(_ => CreateMock<IFormConfigurationService>().Object);
        services.AddScoped<IFormValidationService>(_ => CreateMock<IFormValidationService>().Object);
        services.AddScoped<IFormDataExtractionService>(_ => CreateMock<IFormDataExtractionService>().Object);
        services.AddScoped<IEmailTemplateService>(_ => CreateMock<IEmailTemplateService>().Object);
        services.AddScoped<IConnectorFactory>(_ => CreateMock<IConnectorFactory>().Object);
        services.AddScoped<IConnectorService>(_ => CreateMock<IConnectorService>().Object);
        services.AddSingleton<IBackgroundTaskQueue>(_ => CreateMock<IBackgroundTaskQueue>().Object);
        
        // Add real implementations for services that tests typically want to test
        services.AddScoped<FormSubmissionService>();
        
        ConfigureServices(services);
        
        ServiceProvider = services.BuildServiceProvider();
        
        // Get DbContext from service provider
        DbContext = ServiceProvider.GetRequiredService<AppDbContext>();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Override in derived classes to add specific services or replace mocks with real implementations
    }

    protected T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();
    
    protected Mock<T> CreateMock<T>() where T : class => new Mock<T>();
    
    protected void EnsureDatabaseCreated()
    {
        try 
        {
            DbContext.Database.EnsureCreated();
        }
        catch (System.InvalidOperationException)
        {
            // If we get any dependency injection error, create a new context without the problematic setup
            // and ensure the database is created with that context
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            
            using var tempContext = new AppDbContext(options);
            tempContext.Database.EnsureCreated();
        }
    }

    public void Dispose()
    {
        ServiceProvider?.Dispose();
        DbContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}