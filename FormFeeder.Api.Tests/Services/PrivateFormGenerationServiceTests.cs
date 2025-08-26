using FormFeeder.Api.Services;
using FormFeeder.Api.Services.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FormFeeder.Api.Tests.Services;

public sealed class PrivateFormGenerationServiceTests
{
    private readonly Mock<IFormIdGenerationService> _mockFormIdGenerator;
    private readonly Mock<ILogger<PrivateFormGenerationService>> _mockLogger;
    private readonly MailJetDefaults _mailJetDefaults;
    private readonly PrivateFormGenerationService _service;

    public PrivateFormGenerationServiceTests()
    {
        _mockFormIdGenerator = new Mock<IFormIdGenerationService>();
        _mockLogger = new Mock<ILogger<PrivateFormGenerationService>>();
        
        _mailJetDefaults = new MailJetDefaults
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret",
            FromEmail = "test@example.com",
            FromName = "Test Sender",
            Subject = "Test Subject",
            TemplateId = "test-template-id"
        };

        var mockOptions = new Mock<IOptions<MailJetDefaults>>();
        mockOptions.Setup(x => x.Value).Returns(_mailJetDefaults);

        _service = new PrivateFormGenerationService(
            _mockFormIdGenerator.Object,
            mockOptions.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GeneratePrivateFormAsync_WithValidEmail_ReturnsCorrectConfiguration()
    {
        // Arrange
        const string email = "recipient@example.com";
        const string expectedFormId = "prv-abc123xyz";
        _mockFormIdGenerator.Setup(x => x.GeneratePrivateFormId()).Returns(expectedFormId);

        // Act
        var result = await _service.GeneratePrivateFormAsync(email);

        // Assert
        result.Should().NotBeNull();
        result.FormId.Should().Be(expectedFormId);
        result.PrivacyMode.Should().BeTrue();
        result.Enabled.Should().BeTrue();
        result.AllowedDomains.Should().ContainSingle("*");
        result.Description.Should().Be($"Auto-generated private form for {email}");
        
        result.RateLimit.Should().NotBeNull();
        result.RateLimit!.RequestsPerWindow.Should().Be(100);
        result.RateLimit.WindowMinutes.Should().Be(1);
        
        result.Connectors.Should().ContainSingle();
        var connector = result.Connectors!.Single();
        connector.Type.Should().Be("MailJet");
        connector.Name.Should().Be("PrivateFormNotification");
        connector.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task GeneratePrivateFormAsync_WithValidEmail_ConfiguresMailJetConnectorCorrectly()
    {
        // Arrange
        const string email = "recipient@example.com";
        _mockFormIdGenerator.Setup(x => x.GeneratePrivateFormId()).Returns("prv-test123");

        // Act
        var result = await _service.GeneratePrivateFormAsync(email);

        // Assert
        var connector = result.Connectors!.Single();
        connector.Settings.Should().ContainKeys("ApiKey", "ApiSecret", "FromEmail", "FromName", "ToEmail", "ToName", "Subject", "TemplateId");
        
        connector.Settings["ApiKey"].Should().Be(_mailJetDefaults.ApiKey);
        connector.Settings["ApiSecret"].Should().Be(_mailJetDefaults.ApiSecret);
        connector.Settings["FromEmail"].Should().Be(_mailJetDefaults.FromEmail);
        connector.Settings["FromName"].Should().Be(_mailJetDefaults.FromName);
        connector.Settings["ToEmail"].Should().Be(email);
        connector.Settings["ToName"].Should().Be("Recipient");
        connector.Settings["Subject"].Should().Be(_mailJetDefaults.Subject);
        connector.Settings["TemplateId"].Should().Be(_mailJetDefaults.TemplateId);
    }

    [Fact]
    public async Task GeneratePrivateFormAsync_CallsFormIdGenerator()
    {
        // Arrange
        const string email = "recipient@example.com";
        _mockFormIdGenerator.Setup(x => x.GeneratePrivateFormId()).Returns("prv-test123");

        // Act
        await _service.GeneratePrivateFormAsync(email);

        // Assert
        _mockFormIdGenerator.Verify(x => x.GeneratePrivateFormId(), Times.Once);
    }

    [Fact]
    public async Task GeneratePrivateFormAsync_LogsFormCreation()
    {
        // Arrange
        const string email = "recipient@example.com";
        const string formId = "prv-test123";
        _mockFormIdGenerator.Setup(x => x.GeneratePrivateFormId()).Returns(formId);

        // Act
        await _service.GeneratePrivateFormAsync(email);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Generated private form configuration {formId} for recipient {email}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GeneratePrivateFormAsync_WithInvalidEmail_ThrowsArgumentException(string invalidEmail)
    {
        // Act & Assert
        var act = async () => await _service.GeneratePrivateFormAsync(invalidEmail);
        
        await act.Should().ThrowAsync<ArgumentException>();
        _mockFormIdGenerator.Verify(x => x.GeneratePrivateFormId(), Times.Never);
    }
    
    [Fact]
    public async Task GeneratePrivateFormAsync_WithNullEmail_ThrowsArgumentException()
    {
        // Act & Assert
        var act = async () => await _service.GeneratePrivateFormAsync(null!);
        
        await act.Should().ThrowAsync<ArgumentException>();
        _mockFormIdGenerator.Verify(x => x.GeneratePrivateFormId(), Times.Never);
    }

    [Fact]
    public async Task GeneratePrivateFormAsync_EnsuresPrivacyModeIsValid()
    {
        // Arrange
        const string email = "recipient@example.com";
        _mockFormIdGenerator.Setup(x => x.GeneratePrivateFormId()).Returns("prv-test123");

        // Act
        var result = await _service.GeneratePrivateFormAsync(email);

        // Assert - Privacy mode is valid if there's at least one enabled connector
        result.IsPrivacyModeValid().Should().BeTrue();
        result.HasEnabledConnectors().Should().BeTrue();
    }
}