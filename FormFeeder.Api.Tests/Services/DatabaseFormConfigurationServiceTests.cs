using FormFeeder.Api.Connectors;
using FormFeeder.Api.Models;
using FormFeeder.Api.Models.Entities;
using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FormFeeder.Api.Tests.Services;

public class DatabaseFormConfigurationServiceTests : TestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<DatabaseFormConfigurationService>();
    }

    private async Task<FormConfigurationEntity> CreateTestFormConfigurationEntityAsync()
    {
        var entity = new FormConfigurationEntity
        {
            FormId = "test-form",
            Description = "Test form description",
            Enabled = true,
            PrivacyMode = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AllowedDomains = new List<AllowedDomainEntity>
            {
                new() { Domain = "example.com" },
                new() { Domain = "test.com" }
            },
            RateLimit = new RateLimitSettingsEntity
            {
                RequestsPerWindow = 10,
                WindowMinutes = 1
            },
            Connectors = new List<ConnectorConfigurationEntity>
            {
                new()
                {
                    Type = "MailJet",
                    Name = "TestConnector",
                    Enabled = true,
                    SettingsJson = """{"apiKey":"test","apiSecret":"secret"}"""
                }
            }
        };

        DbContext.FormConfigurations.Add(entity);
        await DbContext.SaveChangesAsync();
        
        return entity;
    }

    public class GetFormConfigurationAsync : DatabaseFormConfigurationServiceTests
    {
        [Fact]
        public async Task GetFormConfigurationAsync_WithExistingFormId_ShouldReturnConfiguration()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            await CreateTestFormConfigurationEntityAsync();

            // Act
            var result = await service.GetFormConfigurationAsync("test-form");

            // Assert
            result.Should().NotBeNull();
            result!.FormId.Should().Be("test-form");
            result.Description.Should().Be("Test form description");
            result.Enabled.Should().BeTrue();
            result.PrivacyMode.Should().BeFalse();
            result.AllowedDomains.Should().HaveCount(2);
            result.AllowedDomains.Should().Contain("example.com");
            result.AllowedDomains.Should().Contain("test.com");
            result.RateLimit.Should().NotBeNull();
            result.RateLimit!.RequestsPerWindow.Should().Be(10);
            result.RateLimit.WindowMinutes.Should().Be(1);
            result.Connectors.Should().HaveCount(1);
            result.Connectors![0].Type.Should().Be("MailJet");
            result.Connectors[0].Name.Should().Be("TestConnector");
            result.Connectors[0].Enabled.Should().BeTrue();
        }

        [Fact]
        public async Task GetFormConfigurationAsync_WithNonExistentFormId_ShouldReturnNull()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();

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
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            await CreateTestFormConfigurationEntityAsync();

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
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();

            // Act
            var result = await service.GetFormConfigurationAsync(formId!);

            // Assert
            result.Should().BeNull();
        }
    }

    public class GetAllFormConfigurationsAsync : DatabaseFormConfigurationServiceTests
    {
        [Fact]
        public async Task GetAllFormConfigurationsAsync_WithMultipleConfigurations_ShouldReturnAll()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            
            // Create multiple test configurations
            await CreateTestFormConfigurationEntityAsync();
            
            var secondEntity = new FormConfigurationEntity
            {
                FormId = "second-form",
                Description = "Second form",
                Enabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedDomains = new List<AllowedDomainEntity> { new() { Domain = "*" } }
            };
            DbContext.FormConfigurations.Add(secondEntity);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await service.GetAllFormConfigurationsAsync();

            // Assert
            var configurations = result.ToList();
            configurations.Should().HaveCount(2);
            configurations.Should().Contain(c => c.FormId == "test-form");
            configurations.Should().Contain(c => c.FormId == "second-form");
        }

        [Fact]
        public async Task GetAllFormConfigurationsAsync_WithEmptyDatabase_ShouldReturnEmptyCollection()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();

            // Act
            var result = await service.GetAllFormConfigurationsAsync();

            // Assert
            result.Should().BeEmpty();
        }
    }

    public class IsFormEnabledAsync : DatabaseFormConfigurationServiceTests
    {
        [Fact]
        public async Task IsFormEnabledAsync_WithEnabledForm_ShouldReturnTrue()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            await CreateTestFormConfigurationEntityAsync();

            // Act
            var result = await service.IsFormEnabledAsync("test-form");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsFormEnabledAsync_WithDisabledForm_ShouldReturnFalse()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            
            var entity = new FormConfigurationEntity
            {
                FormId = "disabled-form",
                Enabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            DbContext.FormConfigurations.Add(entity);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await service.IsFormEnabledAsync("disabled-form");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsFormEnabledAsync_WithNonExistentForm_ShouldReturnFalse()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();

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
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();

            // Act
            var result = await service.IsFormEnabledAsync(formId!);

            // Assert
            result.Should().BeFalse();
        }
    }

    public class IsDomainAllowedForFormAsync : DatabaseFormConfigurationServiceTests
    {
        [Theory]
        [InlineData("example.com")]
        [InlineData("test.com")]
        public async Task IsDomainAllowedForFormAsync_WithAllowedDomain_ShouldReturnTrue(string domain)
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            await CreateTestFormConfigurationEntityAsync();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("test-form", domain);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsDomainAllowedForFormAsync_WithWildcardDomain_ShouldAllowAnyDomain()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            
            var entity = new FormConfigurationEntity
            {
                FormId = "wildcard-form",
                Enabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedDomains = new List<AllowedDomainEntity> { new() { Domain = "*" } }
            };
            DbContext.FormConfigurations.Add(entity);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("wildcard-form", "anydomain.com");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsDomainAllowedForFormAsync_WithDisallowedDomain_ShouldReturnFalse()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            await CreateTestFormConfigurationEntityAsync();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("test-form", "blocked.com");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsDomainAllowedForFormAsync_WithDisabledForm_ShouldReturnFalse()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            
            var entity = new FormConfigurationEntity
            {
                FormId = "disabled-form",
                Enabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedDomains = new List<AllowedDomainEntity> { new() { Domain = "allowed.com" } }
            };
            DbContext.FormConfigurations.Add(entity);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("disabled-form", "allowed.com");

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
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            await CreateTestFormConfigurationEntityAsync();

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
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            await CreateTestFormConfigurationEntityAsync();

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
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            await CreateTestFormConfigurationEntityAsync();

            // Act
            var result = await service.IsDomainAllowedForFormAsync("test-form", domain);

            // Assert
            result.Should().BeTrue();
        }
    }

    public class GetAllAllowedDomainsAsync : DatabaseFormConfigurationServiceTests
    {
        [Fact]
        public async Task GetAllAllowedDomainsAsync_ShouldReturnAllDomainsFromEnabledForms()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            
            // Create enabled form
            await CreateTestFormConfigurationEntityAsync();
            
            // Create disabled form
            var disabledEntity = new FormConfigurationEntity
            {
                FormId = "disabled-form",
                Enabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedDomains = new List<AllowedDomainEntity> { new() { Domain = "disabled.com" } }
            };
            DbContext.FormConfigurations.Add(disabledEntity);
            
            // Create form with wildcard domain
            var wildcardEntity = new FormConfigurationEntity
            {
                FormId = "wildcard-form",
                Enabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedDomains = new List<AllowedDomainEntity> { new() { Domain = "*" } }
            };
            DbContext.FormConfigurations.Add(wildcardEntity);
            
            await DbContext.SaveChangesAsync();

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
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            
            var entity1 = new FormConfigurationEntity
            {
                FormId = "form1",
                Enabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedDomains = new List<AllowedDomainEntity> 
                { 
                    new() { Domain = "duplicate.com" },
                    new() { Domain = "unique1.com" }
                }
            };
            
            var entity2 = new FormConfigurationEntity
            {
                FormId = "form2",
                Enabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedDomains = new List<AllowedDomainEntity> 
                { 
                    new() { Domain = "duplicate.com" },
                    new() { Domain = "unique2.com" }
                }
            };
            
            DbContext.FormConfigurations.AddRange(entity1, entity2);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await service.GetAllAllowedDomainsAsync();

            // Assert
            var domainList = result.ToList();
            domainList.Should().HaveCount(3);
            domainList.Should().Contain("duplicate.com");
            domainList.Should().Contain("unique1.com");
            domainList.Should().Contain("unique2.com");
        }

        [Fact]
        public async Task GetAllAllowedDomainsAsync_ShouldBeCaseInsensitive()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            
            var entity = new FormConfigurationEntity
            {
                FormId = "test-form",
                Enabled = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedDomains = new List<AllowedDomainEntity> { new() { Domain = "Example.COM" } }
            };
            DbContext.FormConfigurations.Add(entity);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await service.GetAllAllowedDomainsAsync();

            // Assert
            result.Contains("EXAMPLE.COM").Should().BeTrue();
            result.Contains("example.com").Should().BeTrue();
        }
    }

    public class CreateFormConfigurationAsync : DatabaseFormConfigurationServiceTests
    {
        [Fact]
        public async Task CreateFormConfigurationAsync_WithValidConfiguration_ShouldCreateAndReturn()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            
            var config = new FormConfiguration
            {
                FormId = "new-form",
                Description = "New form description",
                Enabled = true,
                AllowedDomains = new List<string> { "new.com", "example.com" },
                RateLimit = new RateLimitSettings { RequestsPerWindow = 5, WindowMinutes = 2 },
                Connectors = new List<ConnectorConfiguration>
                {
                    new("Slack", "NewConnector", true, new Dictionary<string, object> { ["webhook"] = "test-url" })
                }
            };

            // Act
            var result = await service.CreateFormConfigurationAsync(config);

            // Assert
            result.Should().NotBeNull();
            result.FormId.Should().Be("new-form");
            
            // Verify it was saved to database
            var saved = await DbContext.FormConfigurations
                .Include(f => f.AllowedDomains)
                .Include(f => f.RateLimit)
                .Include(f => f.Connectors)
                .FirstAsync(f => f.FormId == "new-form");
                
            saved.Should().NotBeNull();
            saved.AllowedDomains.Should().HaveCount(2);
            saved.RateLimit.Should().NotBeNull();
            saved.Connectors.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateFormConfigurationAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();

            // Act & Assert
            await service.Invoking(s => s.CreateFormConfigurationAsync(null!))
                .Should().ThrowAsync<ArgumentNullException>();
        }
    }

    public class UpdateFormConfigurationAsync : DatabaseFormConfigurationServiceTests
    {
        [Fact]
        public async Task UpdateFormConfigurationAsync_WithExistingConfiguration_ShouldUpdateAndReturn()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            var originalEntity = await CreateTestFormConfigurationEntityAsync();

            var updatedConfig = new FormConfiguration
            {
                FormId = "test-form",
                Description = "Updated description",
                Enabled = false,
                PrivacyMode = true,
                AllowedDomains = new List<string> { "updated.com" },
                RateLimit = new RateLimitSettings { RequestsPerWindow = 20, WindowMinutes = 5 }
            };

            // Act
            var result = await service.UpdateFormConfigurationAsync(updatedConfig);

            // Assert
            result.Should().NotBeNull();
            result!.FormId.Should().Be("test-form");
            result.Description.Should().Be("Updated description");
            result.Enabled.Should().BeFalse();
            result.PrivacyMode.Should().BeTrue();
            result.AllowedDomains.Should().HaveCount(1);
            result.AllowedDomains.Should().Contain("updated.com");
            result.RateLimit!.RequestsPerWindow.Should().Be(20);
            result.RateLimit.WindowMinutes.Should().Be(5);
        }

        [Fact]
        public async Task UpdateFormConfigurationAsync_WithNonExistentConfiguration_ShouldReturnNull()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            
            var config = new FormConfiguration
            {
                FormId = "non-existent",
                Description = "Test"
            };

            // Act
            var result = await service.UpdateFormConfigurationAsync(config);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateFormConfigurationAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();

            // Act & Assert
            await service.Invoking(s => s.UpdateFormConfigurationAsync(null!))
                .Should().ThrowAsync<ArgumentNullException>();
        }
    }

    public class DeleteFormConfigurationAsync : DatabaseFormConfigurationServiceTests
    {
        [Fact]
        public async Task DeleteFormConfigurationAsync_WithExistingConfiguration_ShouldReturnTrue()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();
            await CreateTestFormConfigurationEntityAsync();

            // Act
            var result = await service.DeleteFormConfigurationAsync("test-form");

            // Assert
            result.Should().BeTrue();
            
            // Verify it was deleted from database
            var exists = await DbContext.FormConfigurations.AnyAsync(f => f.FormId == "test-form");
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteFormConfigurationAsync_WithNonExistentConfiguration_ShouldReturnFalse()
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();

            // Act
            var result = await service.DeleteFormConfigurationAsync("non-existent");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteFormConfigurationAsync_WithInvalidFormId_ShouldReturnFalse(string? formId)
        {
            // Arrange
            EnsureDatabaseCreated();
            var service = GetService<DatabaseFormConfigurationService>();

            // Act
            var result = await service.DeleteFormConfigurationAsync(formId!);

            // Assert
            result.Should().BeFalse();
        }
    }
}