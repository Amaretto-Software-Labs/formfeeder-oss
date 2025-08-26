using System.Net;
using System.Text;
using System.Text.Json;
using FormFeeder.Api.Connectors;
using FormFeeder.Api.Models;
using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Polly;

namespace FormFeeder.Api.Tests.Connectors;

public class SlackConnectorTests : TestBase
{
    private readonly SlackConnector _connector;
    private readonly Mock<ILogger<SlackConnector>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IRetryPolicyFactory> _retryPolicyFactoryMock;
    private readonly HttpClient _httpClient;

    public SlackConnectorTests()
    {
        _loggerMock = CreateMock<ILogger<SlackConnector>>();
        _httpClientFactoryMock = CreateMock<IHttpClientFactory>();
        _httpMessageHandlerMock = CreateMock<HttpMessageHandler>();
        _retryPolicyFactoryMock = CreateMock<IRetryPolicyFactory>();
        
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                             .Returns(_httpClient);

        // Setup retry policy mock to return a pass-through policy (no retry) for testing
        var passThoughPolicy = ResiliencePipeline.Empty;
        _retryPolicyFactoryMock.Setup(x => x.CreateSlackRetryPolicy())
                              .Returns(passThoughPolicy);
        
        _connector = new SlackConnector(
            _loggerMock.Object, 
            _httpClientFactoryMock.Object,
            _retryPolicyFactoryMock.Object,
            "test-connector");
    }

    ~SlackConnectorTests()
    {
        _httpClient?.Dispose();
    }

    public class Properties : SlackConnectorTests
    {
        [Fact]
        public void Type_ShouldReturnSlack()
        {
            // Assert
            _connector.Type.Should().Be("Slack");
        }

        [Fact]
        public void Name_ShouldReturnProvidedName()
        {
            // Assert
            _connector.Name.Should().Be("test-connector");
        }

        [Fact]
        public void Name_WithDefaultConstructor_ShouldReturnSlack()
        {
            // Arrange
            var defaultConnector = new SlackConnector(_loggerMock.Object, _httpClientFactoryMock.Object, _retryPolicyFactoryMock.Object);

            // Assert
            defaultConnector.Name.Should().Be("Slack");
        }

        [Fact]
        public void Enabled_ShouldBeSettableAndGettable()
        {
            // Arrange
            _connector.Enabled = false;

            // Act & Assert
            _connector.Enabled.Should().BeFalse();

            _connector.Enabled = true;
            _connector.Enabled.Should().BeTrue();
        }
    }

    public class ExecuteAsync : SlackConnectorTests
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
            result.Message.Should().Be("Slack configuration is missing");
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyConfiguration_ShouldReturnFailure()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>();

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Slack webhook URL is missing");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ExecuteAsync_WithMissingWebhookUrl_ShouldReturnFailure(string? webhookUrl)
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["WebhookUrl"] = webhookUrl!
            };

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Slack webhook URL is missing");
        }

        [Theory]
        [InlineData("not-a-url")]
        [InlineData("ftp://invalid.com")]
        [InlineData("invalid://webhook")]
        public async Task ExecuteAsync_WithInvalidWebhookUrl_ShouldReturnFailure(string invalidUrl)
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["WebhookUrl"] = invalidUrl
            };

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Invalid Slack webhook URL");
        }

        [Theory]
        [InlineData("https://hooks.slack.com/services/test")]
        [InlineData("http://localhost:3000/webhook")]
        public async Task ExecuteAsync_WithValidWebhookUrl_ShouldAcceptUrl(string validUrl)
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["WebhookUrl"] = validUrl
            };

            SetupSuccessfulHttpResponse();

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulResponse_ShouldReturnSuccess()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission("contact-form");
            var config = CreateValidConfiguration();
            
            SetupSuccessfulHttpResponse("ok");

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Slack notification sent");
            result.Metadata.Should().ContainKey("Response");
            result.Metadata!["Response"].Should().Be("ok");
        }

        [Fact]
        public async Task ExecuteAsync_WithHttpError_ShouldReturnFailure()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = CreateValidConfiguration();
            
            SetupHttpErrorResponse(HttpStatusCode.BadRequest, "invalid_payload");

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Failed to send Slack notification");
            result.Message.Should().Contain("BadRequest");
            result.Message.Should().Contain("invalid_payload");
        }

        [Fact]
        public async Task ExecuteAsync_WithNetworkException_ShouldReturnFailureWithException()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = CreateValidConfiguration();
            
            var exception = new HttpRequestException("Network error");
            SetupHttpException(exception);

            // Act
            var result = await _connector.ExecuteAsync(submission, config);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Failed to send Slack notification");
            result.Message.Should().Contain("Network error");
        }


        [Fact]
        public async Task ExecuteAsync_ShouldSendCorrectHttpRequest()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = CreateValidConfiguration();
            var webhookUrl = config["WebhookUrl"].ToString()!;
            
            SetupSuccessfulHttpResponse();

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            VerifyHttpRequest(HttpMethod.Post, webhookUrl, "application/json");
        }
    }

    public class ConfigurationParsing : SlackConnectorTests
    {
        [Fact]
        public async Task ExecuteAsync_WithAllOptionalParameters_ShouldUseProvidedValues()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["WebhookUrl"] = "https://hooks.slack.com/services/test",
                ["Channel"] = "#general",
                ["Username"] = "CustomBot",
                ["IconEmoji"] = ":robot_face:",
                ["IncludeRawJson"] = "true"
            };
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            slackMessage.RootElement.GetProperty("channel").GetString().Should().Be("#general");
            slackMessage.RootElement.GetProperty("username").GetString().Should().Be("CustomBot");
            slackMessage.RootElement.GetProperty("icon_emoji").GetString().Should().Be(":robot_face:");
            
            // Should include raw JSON attachment
            var attachments = slackMessage.RootElement.GetProperty("attachments");
            attachments.GetArrayLength().Should().Be(3); // Form fields, metadata, raw JSON
            
            var rawJsonAttachment = attachments[2];
            rawJsonAttachment.GetProperty("title").GetString().Should().Be("Raw JSON Data");
        }

        [Fact]
        public async Task ExecuteAsync_WithDefaultValues_ShouldUseDefaults()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["WebhookUrl"] = "https://hooks.slack.com/services/test"
                // All other values should use defaults
            };
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            slackMessage.RootElement.GetProperty("username").GetString().Should().Be("FormFeeder");
            slackMessage.RootElement.GetProperty("icon_emoji").GetString().Should().Be(":envelope:");
            slackMessage.RootElement.TryGetProperty("channel", out var channelProperty).Should().BeTrue();
            channelProperty.ValueKind.Should().Be(JsonValueKind.Null);
            
            // Should not include raw JSON attachment
            var attachments = slackMessage.RootElement.GetProperty("attachments");
            attachments.GetArrayLength().Should().Be(2); // Form fields and metadata only
        }

        [Theory]
        [InlineData("TRUE")]
        [InlineData("True")]
        [InlineData("true")]
        [InlineData("1")]
        public async Task ExecuteAsync_WithIncludeRawJsonTrueVariations_ShouldIncludeRawJson(string trueValue)
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["WebhookUrl"] = "https://hooks.slack.com/services/test",
                ["IncludeRawJson"] = trueValue
            };
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var attachments = slackMessage.RootElement.GetProperty("attachments");
            
            if (trueValue.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                attachments.GetArrayLength().Should().Be(3); // Should include raw JSON
            }
            else
            {
                attachments.GetArrayLength().Should().Be(2); // Should not include raw JSON for non-"true" values
            }
        }

        [Theory]
        [InlineData("FALSE")]
        [InlineData("false")]
        [InlineData("0")]
        [InlineData("no")]
        [InlineData("")]
        public async Task ExecuteAsync_WithIncludeRawJsonFalseVariations_ShouldNotIncludeRawJson(string falseValue)
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["WebhookUrl"] = "https://hooks.slack.com/services/test",
                ["IncludeRawJson"] = falseValue
            };
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var attachments = slackMessage.RootElement.GetProperty("attachments");
            attachments.GetArrayLength().Should().Be(2); // Should not include raw JSON
        }

        [Theory]
        [InlineData(123)]
        [InlineData(123.45)]
        [InlineData(true)]
        public async Task ExecuteAsync_WithNonStringConfigValues_ShouldConvertToString(object value)
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            var config = new Dictionary<string, object>
            {
                ["WebhookUrl"] = "https://hooks.slack.com/services/test",
                ["Username"] = value
            };
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            slackMessage.RootElement.GetProperty("username").GetString().Should().Be(value.ToString());
        }
    }

    public class MessageFormatting : SlackConnectorTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldFormatBasicMessage()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission("contact-form");
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var attachments = slackMessage.RootElement.GetProperty("attachments");
            
            // First attachment should contain form data
            var formAttachment = attachments[0];
            formAttachment.GetProperty("color").GetString().Should().Be("good");
            formAttachment.GetProperty("fallback").GetString().Should().Be("New form submission: contact-form");
            formAttachment.GetProperty("pretext").GetString().Should().Be("New form submission received");
            formAttachment.GetProperty("title").GetString().Should().Be("Form: contact-form");
            formAttachment.GetProperty("footer").GetString().Should().Be("FormFeeder");
            formAttachment.GetProperty("ts").GetInt64().Should().BePositive();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldFormatFormFields()
        {
            // Arrange
            var formData = new Dictionary<string, object>
            {
                ["firstName"] = "John",
                ["last_name"] = "Doe",
                ["email-address"] = "john@example.com",
                ["message"] = "This is a test message"
            };
            
            var submission = TestDataBuilder.CreateFormSubmission(formData: formData);
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var formAttachment = slackMessage.RootElement.GetProperty("attachments")[0];
            var fields = formAttachment.GetProperty("fields");
            
            fields.GetArrayLength().Should().Be(4);
            
            // Check field name formatting
            var firstNameField = fields[0];
            firstNameField.GetProperty("title").GetString().Should().Be("First Name");
            firstNameField.GetProperty("value").GetString().Should().Be("John");
            firstNameField.GetProperty("short").GetBoolean().Should().BeTrue(); // Short value
            
            var lastNameField = fields[1];
            lastNameField.GetProperty("title").GetString().Should().Be("Last name");
            lastNameField.GetProperty("value").GetString().Should().Be("Doe");
            
            var emailField = fields[2];
            emailField.GetProperty("title").GetString().Should().Be("Email address");
            emailField.GetProperty("value").GetString().Should().Be("john@example.com");
            
            var messageField = fields[3];
            messageField.GetProperty("title").GetString().Should().Be("Message");
            messageField.GetProperty("value").GetString().Should().Be("This is a test message");
            messageField.GetProperty("short").GetBoolean().Should().BeTrue(); // Short value (< 40 chars)
        }

        [Fact]
        public async Task ExecuteAsync_ShouldTruncateLongValues()
        {
            // Arrange
            var longValue = new string('x', 150); // > 100 characters
            var formData = new Dictionary<string, object>
            {
                ["description"] = longValue
            };
            
            var submission = TestDataBuilder.CreateFormSubmission(formData: formData);
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var formAttachment = slackMessage.RootElement.GetProperty("attachments")[0];
            var fields = formAttachment.GetProperty("fields");
            
            var descriptionField = fields[0];
            var truncatedValue = descriptionField.GetProperty("value").GetString()!;
            truncatedValue.Should().EndWith("...");
            truncatedValue.Length.Should().Be(100); // 97 + "..."
        }

        [Fact]
        public async Task ExecuteAsync_ShouldIncludeMetadataAttachment()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission(
                clientIp: "192.168.1.100",
                userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
            );
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var metadataAttachment = slackMessage.RootElement.GetProperty("attachments")[1];
            
            metadataAttachment.GetProperty("color").GetString().Should().Be("#e0e0e0");
            
            var fields = metadataAttachment.GetProperty("fields");
            fields.GetArrayLength().Should().Be(3);
            
            var submittedAtField = fields[0];
            submittedAtField.GetProperty("title").GetString().Should().Be("Submitted At");
            submittedAtField.GetProperty("short").GetBoolean().Should().BeTrue();
            
            var ipField = fields[1];
            ipField.GetProperty("title").GetString().Should().Be("IP Address");
            ipField.GetProperty("value").GetString().Should().Be("192.168.1.100");
            ipField.GetProperty("short").GetBoolean().Should().BeTrue();
            
            var userAgentField = fields[2];
            userAgentField.GetProperty("title").GetString().Should().Be("User Agent");
            userAgentField.GetProperty("short").GetBoolean().Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleNullMetadataValues()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission(
                clientIp: null,
                userAgent: null
            );
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var metadataAttachment = slackMessage.RootElement.GetProperty("attachments")[1];
            var fields = metadataAttachment.GetProperty("fields");
            
            var ipField = fields[1];
            ipField.GetProperty("value").GetString().Should().Be("Unknown");
            
            var userAgentField = fields[2];
            userAgentField.GetProperty("value").GetString().Should().Be("Unknown");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldTruncateUserAgent()
        {
            // Arrange
            var longUserAgent = new string('x', 150); // > 100 characters
            var submission = TestDataBuilder.CreateFormSubmission(userAgent: longUserAgent);
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var metadataAttachment = slackMessage.RootElement.GetProperty("attachments")[1];
            var fields = metadataAttachment.GetProperty("fields");
            var userAgentField = fields[2];
            
            var truncatedUserAgent = userAgentField.GetProperty("value").GetString()!;
            truncatedUserAgent.Should().EndWith("...");
            truncatedUserAgent.Length.Should().Be(100); // 97 + "..."
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleEmptyFormData()
        {
            // Arrange
            var emptyFormData = new Dictionary<string, object>();
            var submission = TestDataBuilder.CreateFormSubmission(formData: emptyFormData);
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var formAttachment = slackMessage.RootElement.GetProperty("attachments")[0];
            var fields = formAttachment.GetProperty("fields");
            
            fields.GetArrayLength().Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleNonStringFormValues()
        {
            // Arrange
            var formData = new Dictionary<string, object>
            {
                ["count"] = 42,
                ["price"] = 19.99,
                ["active"] = true,
                ["tags"] = new[] { "tag1", "tag2" }
            };
            
            var submission = TestDataBuilder.CreateFormSubmission(formData: formData);
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var formAttachment = slackMessage.RootElement.GetProperty("attachments")[0];
            var fields = formAttachment.GetProperty("fields");
            
            fields.GetArrayLength().Should().Be(4);
            
            var countField = fields[0];
            countField.GetProperty("value").GetString().Should().Be("42");
            
            var priceField = fields[1];
            priceField.GetProperty("value").GetString().Should().Be("19.99");
            
            var activeField = fields[2];
            activeField.GetProperty("value").GetString().Should().Be("True");
            
            var tagsField = fields[3];
            tagsField.GetProperty("value").GetString().Should().Contain("tag1").And.Contain("tag2");
        }
    }

    public class FieldNameFormatting : SlackConnectorTests
    {
        [Theory]
        [InlineData("firstName", "First Name")]
        [InlineData("lastName", "Last Name")]
        [InlineData("emailAddress", "Email Address")]
        [InlineData("phoneNumber", "Phone Number")]
        [InlineData("fullName", "Full Name")]
        public async Task ExecuteAsync_ShouldFormatCamelCaseFields(string fieldName, string expectedTitle)
        {
            // Arrange
            var formData = new Dictionary<string, object> { [fieldName] = "test value" };
            var submission = TestDataBuilder.CreateFormSubmission(formData: formData);
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var formAttachment = slackMessage.RootElement.GetProperty("attachments")[0];
            var fields = formAttachment.GetProperty("fields");
            
            fields[0].GetProperty("title").GetString().Should().Be(expectedTitle);
        }

        [Theory]
        [InlineData("first_name", "First name")]
        [InlineData("last_name", "Last name")]
        [InlineData("email_address", "Email address")]
        [InlineData("phone_number", "Phone number")]
        [InlineData("user_id", "User id")]
        public async Task ExecuteAsync_ShouldFormatSnakeCaseFields(string fieldName, string expectedTitle)
        {
            // Arrange
            var formData = new Dictionary<string, object> { [fieldName] = "test value" };
            var submission = TestDataBuilder.CreateFormSubmission(formData: formData);
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var formAttachment = slackMessage.RootElement.GetProperty("attachments")[0];
            var fields = formAttachment.GetProperty("fields");
            
            fields[0].GetProperty("title").GetString().Should().Be(expectedTitle);
        }

        [Theory]
        [InlineData("first-name", "First name")]
        [InlineData("email-address", "Email address")]
        [InlineData("phone-number", "Phone number")]
        public async Task ExecuteAsync_ShouldFormatKebabCaseFields(string fieldName, string expectedTitle)
        {
            // Arrange
            var formData = new Dictionary<string, object> { [fieldName] = "test value" };
            var submission = TestDataBuilder.CreateFormSubmission(formData: formData);
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var formAttachment = slackMessage.RootElement.GetProperty("attachments")[0];
            var fields = formAttachment.GetProperty("fields");
            
            fields[0].GetProperty("title").GetString().Should().Be(expectedTitle);
        }

        [Theory]
        [InlineData("name", "Name")]
        [InlineData("email", "Email")]
        [InlineData("message", "Message")]
        [InlineData("id", "Id")]
        [InlineData("url", "Url")]
        public async Task ExecuteAsync_ShouldFormatSingleWordFields(string fieldName, string expectedTitle)
        {
            // Arrange
            var formData = new Dictionary<string, object> { [fieldName] = "test value" };
            var submission = TestDataBuilder.CreateFormSubmission(formData: formData);
            var config = CreateValidConfiguration();
            
            string? capturedPayload = null;
            SetupHttpRequestCapture(payload => capturedPayload = payload);

            // Act
            await _connector.ExecuteAsync(submission, config);

            // Assert
            capturedPayload.Should().NotBeNull();
            
            var slackMessage = JsonDocument.Parse(capturedPayload!);
            var formAttachment = slackMessage.RootElement.GetProperty("attachments")[0];
            var fields = formAttachment.GetProperty("fields");
            
            fields[0].GetProperty("title").GetString().Should().Be(expectedTitle);
        }
    }

    // Helper methods
    private static Dictionary<string, object> CreateValidConfiguration(
        string webhookUrl = "https://hooks.slack.com/services/test/test/test") =>
        new() { ["WebhookUrl"] = webhookUrl };

    private void SetupSuccessfulHttpResponse(string responseContent = "ok")
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupHttpErrorResponse(HttpStatusCode statusCode, string errorContent)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(errorContent)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupHttpException(Exception exception)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
    }

    private void SetupHttpRequestCapture(Action<string> payloadCapture)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                var payload = await request.Content!.ReadAsStringAsync();
                payloadCapture(payload);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("ok")
                };
            });
    }

    private void VerifyHttpRequest(HttpMethod method, string expectedUrl, string expectedContentType)
    {
        _httpMessageHandlerMock
            .Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.ToString() == expectedUrl &&
                    req.Content!.Headers.ContentType!.MediaType == expectedContentType),
                ItExpr.IsAny<CancellationToken>());
    }

}