using FormFeeder.Api.Models;
using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FormFeeder.Api.Tests.Services;

public class FormConfigurationServiceTests : TestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Configure test configuration
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Forms:0:FormId"] = "test-form",
            ["Forms:0:AllowedDomains:0"] = "example.com",
            ["Forms:0:AllowedDomains:1"] = "test.com",
            ["Forms:0:Description"] = "Test form",
            ["Forms:0:Enabled"] = "true",
            ["Forms:0:RateLimit:RequestsPerWindow"] = "5",
            ["Forms:0:RateLimit:WindowMinutes"] = "2",
            
            ["Forms:1:FormId"] = "disabled-form",
            ["Forms:1:AllowedDomains:0"] = "disabled.com",
            ["Forms:1:Description"] = "Disabled form",
            ["Forms:1:Enabled"] = "false",
            
            ["Forms:2:FormId"] = "wildcard-form",
            ["Forms:2:AllowedDomains:0"] = "*",
            ["Forms:2:Description"] = "Wildcard form",
            ["Forms:2:Enabled"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IFormConfigurationService, FormConfigurationService>();
    }

    public class GetFormConfigurationAsync : FormConfigurationServiceTests
    {
        [Fact]
        public async Task GetFormConfigurationAsync_WithExistingFormId_ShouldReturnConfiguration()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.GetFormConfigurationAsync("test-form");

            // Assert
            result.Should().NotBeNull();
            result!.FormId.Should().Be("test-form");
            result.AllowedDomains.Should().Contain("example.com").And.Contain("test.com");
            result.Description.Should().Be("Test form");
            result.Enabled.Should().BeTrue();
            result.RateLimit.Should().NotBeNull();
            result.RateLimit!.RequestsPerWindow.Should().Be(5);
            result.RateLimit.WindowMinutes.Should().Be(2);
        }

        [Fact]
        public async Task GetFormConfigurationAsync_WithNonExistentFormId_ShouldReturnNull()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.GetFormConfigurationAsync("non-existent-form");

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("TEST-FORM")]
        [InlineData("Test-Form")]
        [InlineData("test-FORM")]
        public async Task GetFormConfigurationAsync_WithDifferentCasing_ShouldReturnConfiguration(string formId)
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.GetFormConfigurationAsync(formId);

            // Assert
            result.Should().NotBeNull();
            result!.FormId.Should().Be("test-form");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetFormConfigurationAsync_WithInvalidFormId_ShouldReturnNull(string? formId)
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.GetFormConfigurationAsync(formId!);

            // Assert
            result.Should().BeNull();
        }
    }

    public class GetAllFormConfigurationsAsync : FormConfigurationServiceTests
    {
        [Fact]
        public async Task GetAllFormConfigurationsAsync_ShouldReturnAllConfigurations()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.GetAllFormConfigurationsAsync();

            // Assert
            var configurations = result.ToList();
            configurations.Should().HaveCount(3);
            configurations.Should().Contain(c => c.FormId == "test-form");
            configurations.Should().Contain(c => c.FormId == "disabled-form");
            configurations.Should().Contain(c => c.FormId == "wildcard-form");
        }
    }

    public class IsFormEnabledAsync : FormConfigurationServiceTests
    {
        [Fact]
        public async Task IsFormEnabledAsync_WithEnabledForm_ShouldReturnTrue()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsFormEnabledAsync("test-form");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsFormEnabledAsync_WithDisabledForm_ShouldReturnFalse()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsFormEnabledAsync("disabled-form");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsFormEnabledAsync_WithNonExistentForm_ShouldReturnFalse()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsFormEnabledAsync("non-existent-form");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task IsFormEnabledAsync_WithInvalidFormId_ShouldReturnFalse(string? formId)
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsFormEnabledAsync(formId!);

            // Assert
            result.Should().BeFalse();
        }
    }

    public class IsDomainAllowedForFormAsync : FormConfigurationServiceTests
    {
        [Theory]
        [InlineData("example.com")]
        [InlineData("test.com")]
        public async Task IsDomainAllowedForFormAsync_WithAllowedDomain_ShouldReturnTrue(string domain)
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("test-form", domain);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsDomainAllowedForFormAsync_WithWildcardForm_ShouldAllowAnyDomain()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("wildcard-form", "anydomain.com");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsDomainAllowedForFormAsync_WithDisallowedDomain_ShouldReturnFalse()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("test-form", "blocked.com");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsDomainAllowedForFormAsync_WithDisabledForm_ShouldReturnFalse()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("disabled-form", "disabled.com");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsDomainAllowedForFormAsync_WithNonExistentForm_ShouldReturnFalse()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("non-existent-form", "example.com");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task IsDomainAllowedForFormAsync_WithInvalidDomain_ShouldReturnFalse(string? domain)
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("test-form", domain);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("EXAMPLE.COM")]
        [InlineData("Example.Com")]
        [InlineData("example.COM")]
        public async Task IsDomainAllowedForFormAsync_WithDifferentCasing_ShouldReturnTrue(string domain)
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("test-form", domain);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("sub.example.com")]
        [InlineData("app.test.com")]
        public async Task IsDomainAllowedForFormAsync_WithSubdomain_ShouldReturnTrue(string domain)
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("test-form", domain);

            // Assert
            result.Should().BeTrue();
        }
    }

    public class GetAllAllowedDomainsAsync : FormConfigurationServiceTests
    {
        [Fact]
        public async Task GetAllAllowedDomainsAsync_ShouldReturnAllDomainsFromEnabledForms()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.GetAllAllowedDomainsAsync();

            // Assert
            result.Should().Contain("example.com");
            result.Should().Contain("test.com");
            result.Should().NotContain("disabled.com"); // From disabled form
            result.Should().NotContain("*"); // Wildcard should be excluded
        }

        [Fact]
        public async Task GetAllAllowedDomainsAsync_ShouldReturnUniqueDomainsOnly()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.GetAllAllowedDomainsAsync();

            // Assert
            var domainList = result.ToList();
            domainList.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task GetAllAllowedDomainsAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            var service = GetService<IFormConfigurationService>();

            // Act
            var result = await service.GetAllAllowedDomainsAsync();

            // Assert
            // The HashSet should be created with case-insensitive comparer
            var hashSet = result;
            hashSet.Contains("EXAMPLE.COM").Should().BeTrue();
            hashSet.Contains("example.com").Should().BeTrue();
        }
    }

    public class ServiceInitialization : FormConfigurationServiceTests
    {
        [Fact]
        public void Constructor_ShouldCreateServiceSuccessfully()
        {
            // Arrange & Act (constructor is called during test setup)
            var service = GetService<IFormConfigurationService>();

            // Assert - verify service was created successfully
            service.Should().NotBeNull();
        }
    }

}

public class FormConfigurationServiceWithEmptyConfigTests : TestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IFormConfigurationService, FormConfigurationService>();
    }

    [Fact]
    public async Task GetAllFormConfigurationsAsync_WithEmptyConfig_ShouldReturnEmptyCollection()
    {
        // Arrange
        var service = GetService<IFormConfigurationService>();

        // Act
        var result = await service.GetAllFormConfigurationsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAllowedDomainsAsync_WithEmptyConfig_ShouldReturnEmptyHashSet()
    {
        // Arrange
        var service = GetService<IFormConfigurationService>();

        // Act
        var result = await service.GetAllAllowedDomainsAsync();

        // Assert
        result.Should().BeEmpty();
    }
}