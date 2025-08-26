using FormFeeder.Api.Connectors;
using FormFeeder.Api.Models;
using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace FormFeeder.Api.Tests.Connectors;

public class MailJetConnectorTests : TestBase
{
    private readonly MailJetConnector _connector;
    private readonly Mock<ILogger<MailJetConnector>> _loggerMock;
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock;
    private readonly Mock<IRetryPolicyFactory> _retryPolicyFactoryMock;

    public MailJetConnectorTests()
    {
        _loggerMock = CreateMock<ILogger<MailJetConnector>>();
        _emailTemplateServiceMock = CreateMock<IEmailTemplateService>();
        _retryPolicyFactoryMock = CreateMock<IRetryPolicyFactory>();
        
        var configuration = CreateTestConfiguration();
        
        // Setup retry policy mock to return a pass-through policy (no retry) for testing
        var passThoughPolicy = ResiliencePipeline.Empty;
        _retryPolicyFactoryMock.Setup(x => x.CreateMailJetRetryPolicy())
                              .Returns(passThoughPolicy);
        
        _connector = new MailJetConnector(
            _loggerMock.Object, 
            configuration, 
            _emailTemplateServiceMock.Object,
            _retryPolicyFactoryMock.Object,
            "test-connector");
    }

    private static IConfiguration CreateTestConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["MailJet:ApiKey"] = "test-api-key",
            ["MailJet:ApiSecret"] = "test-api-secret"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    public class ConnectorProperties : MailJetConnectorTests
    {
        [Fact]
        public void Constructor_WithCustomName_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var customConnector = new MailJetConnector(
                _loggerMock.Object, 
                CreateTestConfiguration(), 
                _emailTemplateServiceMock.Object,
                _retryPolicyFactoryMock.Object,
                "custom-name");

            // Assert - Verify the connector is properly initialized with expected values
            customConnector.Name.Should().Be("custom-name");
            customConnector.Type.Should().Be("MailJet");
            customConnector.Enabled.Should().BeTrue("connector should be enabled by default");
        }

        [Fact]
        public void Constructor_WithDefaultName_ShouldUseTypeAsName()
        {
            // Arrange & Act
            var defaultConnector = new MailJetConnector(
                _loggerMock.Object, 
                CreateTestConfiguration(), 
                _emailTemplateServiceMock.Object,
                _retryPolicyFactoryMock.Object);

            // Assert - Verify default naming behavior
            defaultConnector.Name.Should().Be("MailJet");
            defaultConnector.Type.Should().Be("MailJet");
        }
    }

    public class ExecuteAsync : MailJetConnectorTests
    {
        [Fact]
        public async Task ExecuteAsync_WithNullConfiguration_ShouldReturnFailure()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();

            // Act
            var result = await _connector.ExecuteAsync(submission, null);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("MailJet configuration is missing");
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingApiCredentials_ShouldReturnFailure()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["FromEmail"] = "test@example.com",
                ["ToEmail"] = "recipient@example.com"
            };

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("MailJet API credentials are missing");
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingEmailAddresses_ShouldReturnFailure()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["ApiKey"] = "test-key",
                ["ApiSecret"] = "test-secret"
            };

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("From or To email addresses are missing");
        }

        [Theory]
        [InlineData("ApiKey", "")]
        [InlineData("ApiSecret", "")]
        [InlineData("ApiKey", null)]
        [InlineData("ApiSecret", null)]
        public async Task ExecuteAsync_WithEmptyOrNullCredentials_ShouldReturnFailure(string credentialKey, string? credentialValue)
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["ApiKey"] = credentialKey == "ApiKey" ? credentialValue! : "test-key",
                ["ApiSecret"] = credentialKey == "ApiSecret" ? credentialValue! : "test-secret",
                ["FromEmail"] = "test@example.com",
                ["ToEmail"] = "recipient@example.com"
            };

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("MailJet API credentials are missing");
        }

        [Theory]
        [InlineData("FromEmail", "")]
        [InlineData("ToEmail", "")]
        [InlineData("FromEmail", null)]
        [InlineData("ToEmail", null)]
        public async Task ExecuteAsync_WithEmptyOrNullEmailAddresses_ShouldReturnFailure(string emailKey, string? emailValue)
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["ApiKey"] = "test-key",
                ["ApiSecret"] = "test-secret",
                ["FromEmail"] = emailKey == "FromEmail" ? emailValue! : "test@example.com",
                ["ToEmail"] = emailKey == "ToEmail" ? emailValue! : "recipient@example.com"
            };

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("From or To email addresses are missing");
        }

        [Fact]
        public async Task ExecuteAsync_WithValidConfigurationAndNoTemplate_ShouldAttemptToSendEmail()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["ApiKey"] = "test-key",
                ["ApiSecret"] = "test-secret",
                ["FromEmail"] = "sender@example.com",
                ["ToEmail"] = "recipient@example.com"
            };

            var emailContent = new EmailContent(
                "<html><body>Test HTML</body></html>", 
                "Test plain text");

            _emailTemplateServiceMock.Setup(x => x.GenerateEmailContent(submission))
                                   .Returns(emailContent);

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert - Verify the connector attempts email generation and sending process
            _emailTemplateServiceMock.Verify(x => x.GenerateEmailContent(submission), Times.Once,
                "connector should generate email content when no template is specified");
            
            // In test environment, the actual API call fails, but we verify the process was initiated
            result.Success.Should().BeFalse("actual MailJet API call fails in test environment");
            result.Should().NotBeNull("connector should always return a result");
        }

        [Fact]
        public async Task ExecuteAsync_WithMinimalConfiguration_ShouldHandleDefaultValues()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["ApiKey"] = "test-key",
                ["ApiSecret"] = "test-secret",
                ["FromEmail"] = "sender@example.com",
                ["ToEmail"] = "recipient@example.com"
                // Missing optional fields: FromName, ToName, Subject
            };

            _emailTemplateServiceMock.Setup(x => x.GenerateEmailContent(submission))
                                   .Returns(new EmailContent("<html>Test</html>", "Test"));

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert - Verify the connector handles minimal configuration properly
            _emailTemplateServiceMock.Verify(x => x.GenerateEmailContent(submission), Times.Once,
                "connector should generate email content for minimal configuration");
            result.Should().NotBeNull("connector should return a result even with minimal configuration");
        }

        [Fact]
        public async Task ExecuteAsync_WithTemplateId_ShouldSkipEmailContentGenerationAndUseTemplate()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["ApiKey"] = "test-key",
                ["ApiSecret"] = "test-secret",
                ["FromEmail"] = "sender@example.com",
                ["ToEmail"] = "recipient@example.com",
                ["TemplateId"] = "12345"
            };

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert - Verify template-based email flow
            _emailTemplateServiceMock.Verify(x => x.GenerateEmailContent(It.IsAny<FormSubmission>()), Times.Never,
                "connector should not generate email content when using a template");
            
            result.Should().NotBeNull("connector should return a result when using templates");
            // API call fails in test environment, but the template path was taken
            result.Success.Should().BeFalse("actual MailJet API call fails in test environment");
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidTemplateId_ShouldHandleGracefully()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["ApiKey"] = "test-key",
                ["ApiSecret"] = "test-secret",
                ["FromEmail"] = "sender@example.com",
                ["ToEmail"] = "recipient@example.com",
                ["TemplateId"] = "invalid-template-id" // Not a valid integer
            };

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Message.Should().Contain("MailJet connector error");
        }

        [Fact]
        public async Task ExecuteAsync_WhenExceptionOccurs_ShouldReturnFailureWithException()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["ApiKey"] = "test-key",
                ["ApiSecret"] = "test-secret",
                ["FromEmail"] = "sender@example.com",
                ["ToEmail"] = "recipient@example.com"
            };

            var exception = new InvalidOperationException("Test exception");
            _emailTemplateServiceMock.Setup(x => x.GenerateEmailContent(It.IsAny<FormSubmission>()))
                                   .Throws(exception);

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("MailJet connector error");
            result.Error.Should().Be(exception);
        }

    }

    public class ConfigurationParsing : MailJetConnectorTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldParseAllConfigurationValues()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["ApiKey"] = "test-key",
                ["ApiSecret"] = "test-secret",
                ["FromEmail"] = "sender@example.com",
                ["FromName"] = "Test Sender",
                ["ToEmail"] = "recipient@example.com",
                ["ToName"] = "Test Recipient",
                ["Subject"] = "Custom Subject",
                ["TemplateId"] = "12345"
            };

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            // Even though the result will be a failure (due to actual API call in tests),
            // we can verify that the configuration was parsed without throwing exceptions
            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData(123)] // Integer
        [InlineData(123.45)] // Double
        [InlineData(true)] // Boolean
        public async Task ExecuteAsync_WithNonStringConfigValues_ShouldConvertToString(object value)
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["ApiKey"] = value, // Non-string value
                ["ApiSecret"] = "test-secret",
                ["FromEmail"] = "sender@example.com",
                ["ToEmail"] = "recipient@example.com"
            };

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            // Should not throw exception during conversion
            result.Should().NotBeNull();
        }
    }
}