using FormFeeder.Api.Connectors;
using FormFeeder.Api.Models;
using FormFeeder.Api.Tests.Infrastructure;

namespace FormFeeder.Api.Tests.Models;

public class FormConfigurationTests
{
    public class Properties : FormConfigurationTests
    {
        [Fact]
        public void FormConfiguration_ShouldHaveRequiredProperties()
        {
            // Arrange
            const string formId = "test-form";
            var allowedDomains = new List<string> { "example.com", "test.com" };
            const string description = "Test form configuration";
            const bool enabled = true;
            var rateLimit = new RateLimitSettings { RequestsPerWindow = 10, WindowMinutes = 1 };
            var connectors = new List<ConnectorConfiguration> { TestDataBuilder.CreateConnectorConfiguration() };

            // Act
            var config = new FormConfiguration
            {
                FormId = formId,
                AllowedDomains = allowedDomains,
                Description = description,
                Enabled = enabled,
                RateLimit = rateLimit,
                Connectors = connectors
            };

            // Assert
            config.FormId.Should().Be(formId);
            config.AllowedDomains.Should().BeEquivalentTo(allowedDomains);
            config.Description.Should().Be(description);
            config.Enabled.Should().Be(enabled);
            config.RateLimit.Should().Be(rateLimit);
            config.Connectors.Should().BeEquivalentTo(connectors);
        }

        [Fact]
        public void FormConfiguration_AllowedDomains_ShouldInitializeToEmptyList()
        {
            // Act
            var config = new FormConfiguration { FormId = "test" };

            // Assert
            config.AllowedDomains.Should().NotBeNull();
            config.AllowedDomains.Should().BeEmpty();
        }

        [Fact]
        public void FormConfiguration_Enabled_ShouldDefaultToTrue()
        {
            // Act
            var config = new FormConfiguration { FormId = "test" };

            // Assert
            config.Enabled.Should().BeTrue();
        }

        [Fact]
        public void FormConfiguration_OptionalProperties_ShouldAllowNull()
        {
            // Act
            var config = new FormConfiguration
            {
                FormId = "test",
                Description = null,
                RateLimit = null,
                Connectors = null
            };

            // Assert
            config.Description.Should().BeNull();
            config.RateLimit.Should().BeNull();
            config.Connectors.Should().BeNull();
        }

        [Fact]
        public void FormConfiguration_FormIdProperty_ShouldBeRequired()
        {
            // Arrange & Act
            var config = new FormConfiguration { FormId = "test" };

            // Assert
            config.FormId.Should().NotBeNull(); // Required property is initialized
        }
    }

    public class AllowedDomainsHandling : FormConfigurationTests
    {
        [Fact]
        public void FormConfiguration_AllowedDomains_ShouldSupportMultipleDomains()
        {
            // Arrange
            var domains = new List<string> { "example.com", "subdomain.example.com", "test.org", "localhost" };

            // Act
            var config = new FormConfiguration
            {
                FormId = "multi-domain-form",
                AllowedDomains = domains
            };

            // Assert
            config.AllowedDomains.Should().HaveCount(4);
            config.AllowedDomains.Should().Contain(domains);
        }

        [Fact]
        public void FormConfiguration_AllowedDomains_ShouldSupportWildcard()
        {
            // Arrange
            var domains = new List<string> { "*" };

            // Act
            var config = new FormConfiguration
            {
                FormId = "wildcard-form",
                AllowedDomains = domains
            };

            // Assert
            config.AllowedDomains.Should().Contain("*");
        }

        [Fact]
        public void FormConfiguration_AllowedDomains_ShouldAllowEmptyList()
        {
            // Arrange & Act
            var config = new FormConfiguration
            {
                FormId = "no-domains-form",
                AllowedDomains = []
            };

            // Assert
            config.AllowedDomains.Should().BeEmpty();
        }

        [Fact]
        public void FormConfiguration_AllowedDomains_ShouldSupportDuplicates()
        {
            // Arrange
            var domains = new List<string> { "example.com", "example.com", "test.com" };

            // Act
            var config = new FormConfiguration
            {
                FormId = "duplicate-domains-form",
                AllowedDomains = domains
            };

            // Assert
            config.AllowedDomains.Should().HaveCount(3);
            config.AllowedDomains.Count(d => d == "example.com").Should().Be(2);
        }
    }

    public class RateLimitSettingsHandling : FormConfigurationTests
    {
        [Fact]
        public void FormConfiguration_WithCustomRateLimit_ShouldUseCustomValues()
        {
            // Arrange
            var rateLimit = new RateLimitSettings
            {
                RequestsPerWindow = 50,
                WindowMinutes = 5
            };

            // Act
            var config = new FormConfiguration
            {
                FormId = "custom-rate-limit-form",
                RateLimit = rateLimit
            };

            // Assert
            config.RateLimit.Should().NotBeNull();
            config.RateLimit!.RequestsPerWindow.Should().Be(50);
            config.RateLimit.WindowMinutes.Should().Be(5);
        }

        [Fact]
        public void FormConfiguration_WithoutRateLimit_ShouldAllowNull()
        {
            // Act
            var config = new FormConfiguration
            {
                FormId = "no-rate-limit-form",
                RateLimit = null
            };

            // Assert
            config.RateLimit.Should().BeNull();
        }
    }

    public class ConnectorsHandling : FormConfigurationTests
    {
        [Fact]
        public void FormConfiguration_WithMultipleConnectors_ShouldSupportList()
        {
            // Arrange
            var connectors = new List<ConnectorConfiguration>
            {
                TestDataBuilder.CreateConnectorConfiguration("MailJet", "mailjet-primary", true),
                TestDataBuilder.CreateConnectorConfiguration("MailJet", "mailjet-secondary", false)
            };

            // Act
            var config = new FormConfiguration
            {
                FormId = "multi-connector-form",
                Connectors = connectors
            };

            // Assert
            config.Connectors.Should().HaveCount(2);
            config.Connectors![0].Name.Should().Be("mailjet-primary");
            config.Connectors![0].Enabled.Should().BeTrue();
            config.Connectors![1].Name.Should().Be("mailjet-secondary");
            config.Connectors![1].Enabled.Should().BeFalse();
        }

        [Fact]
        public void FormConfiguration_WithoutConnectors_ShouldAllowNull()
        {
            // Act
            var config = new FormConfiguration
            {
                FormId = "no-connectors-form",
                Connectors = null
            };

            // Assert
            config.Connectors.Should().BeNull();
        }

        [Fact]
        public void FormConfiguration_WithEmptyConnectorsList_ShouldBeValid()
        {
            // Act
            var config = new FormConfiguration
            {
                FormId = "empty-connectors-form",
                Connectors = []
            };

            // Assert
            config.Connectors.Should().NotBeNull();
            config.Connectors.Should().BeEmpty();
        }
    }

    public class Builder : FormConfigurationTests
    {
        [Fact]
        public void TestDataBuilder_CreateFormConfiguration_ShouldCreateValidInstance()
        {
            // Act
            var config = TestDataBuilder.CreateFormConfiguration();

            // Assert
            config.Should().NotBeNull();
            config.FormId.Should().NotBeNullOrEmpty();
            config.AllowedDomains.Should().NotBeNull();
            config.Enabled.Should().BeTrue();
            config.Description.Should().NotBeNullOrEmpty();
            config.RateLimit.Should().NotBeNull();
        }

        [Fact]
        public void TestDataBuilder_CreateFormConfiguration_WithCustomValues_ShouldUseProvidedValues()
        {
            // Arrange
            const string customFormId = "custom-form";
            var customDomains = new List<string> { "custom.com" };
            const bool customEnabled = false;
            var customRateLimit = new RateLimitSettings { RequestsPerWindow = 20, WindowMinutes = 2 };
            var customConnectors = new List<ConnectorConfiguration> { TestDataBuilder.CreateConnectorConfiguration() };

            // Act
            var config = TestDataBuilder.CreateFormConfiguration(
                formId: customFormId,
                allowedDomains: customDomains,
                enabled: customEnabled,
                rateLimit: customRateLimit,
                connectors: customConnectors);

            // Assert
            config.FormId.Should().Be(customFormId);
            config.AllowedDomains.Should().BeEquivalentTo(customDomains);
            config.Enabled.Should().Be(customEnabled);
            config.RateLimit.Should().Be(customRateLimit);
            config.Connectors.Should().BeEquivalentTo(customConnectors);
        }
    }
}

public class RateLimitSettingsTests
{
    public class Properties : RateLimitSettingsTests
    {
        [Fact]
        public void RateLimitSettings_ShouldHaveDefaultValues()
        {
            // Act
            var settings = new RateLimitSettings();

            // Assert
            settings.RequestsPerWindow.Should().Be(10);
            settings.WindowMinutes.Should().Be(1);
        }

        [Fact]
        public void RateLimitSettings_ShouldAllowCustomValues()
        {
            // Act
            var settings = new RateLimitSettings
            {
                RequestsPerWindow = 100,
                WindowMinutes = 5
            };

            // Assert
            settings.RequestsPerWindow.Should().Be(100);
            settings.WindowMinutes.Should().Be(5);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        public void RateLimitSettings_WithZeroOrNegativeValues_ShouldAllowButMayNotMakeSense(int requests, int minutes)
        {
            // Act
            var settings = new RateLimitSettings
            {
                RequestsPerWindow = requests,
                WindowMinutes = minutes
            };

            // Assert
            settings.RequestsPerWindow.Should().Be(requests);
            settings.WindowMinutes.Should().Be(minutes);
        }

        [Fact]
        public void RateLimitSettings_WithLargeValues_ShouldHandle()
        {
            // Act
            var settings = new RateLimitSettings
            {
                RequestsPerWindow = int.MaxValue,
                WindowMinutes = int.MaxValue
            };

            // Assert
            settings.RequestsPerWindow.Should().Be(int.MaxValue);
            settings.WindowMinutes.Should().Be(int.MaxValue);
        }
    }

    public class Equality : RateLimitSettingsTests
    {
        [Fact]
        public void RateLimitSettings_WithSameValues_ShouldNotBeEqualByReference()
        {
            // Arrange
            var settings1 = new RateLimitSettings { RequestsPerWindow = 10, WindowMinutes = 1 };
            var settings2 = new RateLimitSettings { RequestsPerWindow = 10, WindowMinutes = 1 };

            // Assert
            settings1.Should().NotBeSameAs(settings2); // Different instances
            // Note: RateLimitSettings is a class, not a record, so no value equality
        }

        [Fact]
        public void RateLimitSettings_SameInstance_ShouldBeSameReference()
        {
            // Arrange
            var settings = new RateLimitSettings { RequestsPerWindow = 10, WindowMinutes = 1 };

            // Assert
            settings.Should().BeSameAs(settings);
        }
    }
}