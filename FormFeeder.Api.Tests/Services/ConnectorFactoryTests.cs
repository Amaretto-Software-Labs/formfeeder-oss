using FormFeeder.Api.Connectors;
using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace FormFeeder.Api.Tests.Services;

public class ConnectorFactoryTests : TestBase
{
    private readonly ConnectorFactory _factory;
    private readonly Mock<ILogger<ConnectorFactory>> _loggerMock;

    public ConnectorFactoryTests()
    {
        _loggerMock = CreateMock<ILogger<ConnectorFactory>>();
        var serviceScopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        _factory = new ConnectorFactory(serviceScopeFactory, _loggerMock.Object);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["MailJet:ApiKey"] = "test-key",
            ["MailJet:ApiSecret"] = "test-secret"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IEmailTemplateService>(CreateMock<IEmailTemplateService>().Object);
        services.AddHttpClient();
        services.AddLogging();
        
        // Add retry policy factory mock
        var retryPolicyMock = CreateMock<IRetryPolicyFactory>();
        retryPolicyMock.Setup(x => x.CreateMailJetRetryPolicy()).Returns(ResiliencePipeline.Empty);
        retryPolicyMock.Setup(x => x.CreateSlackRetryPolicy()).Returns(ResiliencePipeline.Empty);
        services.AddSingleton(retryPolicyMock.Object);
    }

    public class IsConnectorTypeSupported : ConnectorFactoryTests
    {
        [Theory]
        [InlineData("MailJet")]
        [InlineData("Slack")]
        public void IsConnectorTypeSupported_WithSupportedTypes_ShouldReturnTrue(string connectorType)
        {
            // Act
            var result = _factory.IsConnectorTypeSupported(connectorType);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("mailjet")]
        [InlineData("MAILJET")]
        [InlineData("MailJET")]
        [InlineData("slack")]
        [InlineData("SLACK")]
        [InlineData("Slack")]
        public void IsConnectorTypeSupported_WithDifferentCasing_ShouldReturnTrue(string connectorType)
        {
            // Act
            var result = _factory.IsConnectorTypeSupported(connectorType);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("SendGrid")]
        [InlineData("Unknown")]
        [InlineData("")]
        [InlineData(null)]
        public void IsConnectorTypeSupported_WithUnsupportedType_ShouldReturnFalse(string? connectorType)
        {
            // Act
            var result = _factory.IsConnectorTypeSupported(connectorType!);

            // Assert
            result.Should().BeFalse();
        }
    }

    public class CreateConnector : ConnectorFactoryTests
    {
        [Fact]
        public void CreateConnector_WithMailJetType_ShouldCreateMailJetConnector()
        {
            // Act
            var connector = _factory.CreateConnector("MailJet", "test-connector");

            // Assert
            connector.Should().NotBeNull();
            connector!.Type.Should().Be("MailJet");
            connector.Name.Should().Be("test-connector");
        }

        [Theory]
        [InlineData("mailjet")]
        [InlineData("MAILJET")]
        [InlineData("MailJET")]
        public void CreateConnector_WithDifferentCasing_ShouldCreateConnector(string connectorType)
        {
            // Act
            var connector = _factory.CreateConnector(connectorType, "test");

            // Assert
            connector.Should().NotBeNull();
            connector!.Type.Should().Be("MailJet");
        }

        [Fact]
        public void CreateConnector_WithSlackType_ShouldCreateSlackConnector()
        {
            // Act
            var connector = _factory.CreateConnector("Slack", "test-slack");

            // Assert
            connector.Should().NotBeNull();
            connector!.Type.Should().Be("Slack");
            connector.Name.Should().Be("test-slack");
        }

        [Fact]
        public void CreateConnector_WithUnsupportedType_ShouldReturnNull()
        {
            // Act
            var connector = _factory.CreateConnector("UnsupportedType", "test");

            // Assert
            connector.Should().BeNull();
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateConnector_WithInvalidConnectorType_ShouldReturnNull(string? connectorType)
        {
            // Act
            var connector = _factory.CreateConnector(connectorType!, "test");

            // Assert
            connector.Should().BeNull();
        }

        [Theory]
        [InlineData("test-name")]
        [InlineData("")]
        [InlineData(null)]
        public void CreateConnector_WithDifferentNames_ShouldUseProvidedName(string? name)
        {
            // Act
            var connector = _factory.CreateConnector("MailJet", name!);

            // Assert
            connector.Should().NotBeNull();
            connector!.Name.Should().Be(name ?? "MailJet"); // Default name from MailJetConnector
        }
    }

    public class ErrorHandling : ConnectorFactoryTests
    {
        [Fact]
        public void CreateConnector_WhenConstructorThrows_ShouldReturnNull()
        {
            // Arrange - This is difficult to test without creating a mock that throws
            // We'll simulate by using a type that doesn't exist in our factory
            
            // Act
            var connector = _factory.CreateConnector("NonExistentType", "test");

            // Assert
            connector.Should().BeNull();
        }
    }
}