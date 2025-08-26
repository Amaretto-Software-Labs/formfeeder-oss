using FormFeeder.Api.Models;
using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FormFeeder.Api.Tests.Services;

public class FormValidationServiceTests : TestBase
{
    private readonly FormValidationService service;
    private readonly Mock<IFormConfigurationService> formConfigServiceMock;
    private readonly Mock<ILogger<FormValidationService>> loggerMock;

    public FormValidationServiceTests()
    {
        formConfigServiceMock = CreateMock<IFormConfigurationService>();
        loggerMock = CreateMock<ILogger<FormValidationService>>();
        service = new FormValidationService(formConfigServiceMock.Object, loggerMock.Object);
    }

    public class ValidateFormAndOriginAsync : FormValidationServiceTests
    {
        [Fact]
        public async Task ValidateFormAndOriginAsync_WithNonExistentForm_ShouldReturnFailure()
        {
            // Arrange
            const string formId = "non-existent-form";
            var httpContext = new DefaultHttpContext();

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync((FormConfiguration?)null);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("does not exist");
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithDisabledForm_ShouldReturnFailure()
        {
            // Arrange
            const string formId = "disabled-form";
            var httpContext = new DefaultHttpContext();
            var disabledForm = TestDataBuilder.CreateFormConfiguration(formId, enabled: false);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(disabledForm);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("disabled");
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithValidFormAndAllowedOrigin_ShouldReturnSuccess()
        {
            // Arrange
            const string formId = "test-form";
            const string origin = "https://example.com";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = origin;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);
            formConfigServiceMock.Setup(x => x.IsDomainAllowedForFormAsync(formId, origin))
                                  .ReturnsAsync(true);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(formId);
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithWildcardFormAndNoOrigin_ShouldReturnSuccess()
        {
            // Arrange
            const string formId = "wildcard-form";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = "null"; // File:// protocol

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["*"]);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithNullOriginAndNoWildcard_ShouldReturnFailure()
        {
            // Arrange
            const string formId = "strict-form";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = "null";

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Origin not allowed");
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithEmptyOriginAndNoWildcard_ShouldReturnFailure()
        {
            // Arrange
            const string formId = "strict-form";
            var httpContext = new DefaultHttpContext();

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Origin not allowed");
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithInvalidOriginUriAndNoWildcard_ShouldReturnFailure()
        {
            // Arrange
            const string formId = "test-form";
            const string invalidOrigin = "not-a-valid-uri";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = invalidOrigin;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Invalid origin format");
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithInvalidOriginButWildcardAllowed_ShouldReturnSuccess()
        {
            // Arrange
            const string formId = "wildcard-form";
            const string invalidOrigin = "not-a-valid-uri";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = invalidOrigin;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["*"]);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithDisallowedOrigin_ShouldReturnFailure()
        {
            // Arrange
            const string formId = "test-form";
            const string origin = "https://blocked.com";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = origin;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);
            formConfigServiceMock.Setup(x => x.IsDomainAllowedForFormAsync(formId, origin))
                                  .ReturnsAsync(false);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Domain not allowed");
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithRefererInsteadOfOrigin_ShouldUseReferer()
        {
            // Arrange
            const string formId = "test-form";
            const string referer = "https://example.com/contact";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Referer"] = referer;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);
            formConfigServiceMock.Setup(x => x.IsDomainAllowedForFormAsync(formId, "https://example.com"))
                                  .ReturnsAsync(true);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("https://example.com")]
        [InlineData("http://localhost:3000")]
        [InlineData("https://sub.example.com:8080")]
        public async Task ValidateFormAndOriginAsync_WithDifferentValidOrigins_ShouldReturnSuccess(string origin)
        {
            // Arrange
            const string formId = "test-form";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = origin;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["*"]);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);
            formConfigServiceMock.Setup(x => x.IsDomainAllowedForFormAsync(formId, It.IsAny<string>()))
                                  .ReturnsAsync(true);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithOriginPriorityOverReferer_ShouldUseOrigin()
        {
            // Arrange
            const string formId = "test-form";
            const string origin = "https://example.com";
            const string referer = "https://different.com";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = origin;
            httpContext.Request.Headers["Referer"] = referer;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);
            formConfigServiceMock.Setup(x => x.IsDomainAllowedForFormAsync(formId, origin))
                                  .ReturnsAsync(true);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeTrue();
            formConfigServiceMock.Verify(x => x.IsDomainAllowedForFormAsync(formId, origin), Times.Once);
            formConfigServiceMock.Verify(x => x.IsDomainAllowedForFormAsync(formId, referer), Times.Never);
        }
    }

    public class PrivacyModeValidation : FormValidationServiceTests
    {
        [Fact]
        public async Task ValidateFormAndOriginAsync_WithValidPrivacyModeForm_ShouldReturnSuccess()
        {
            // Arrange
            const string formId = "privacy-form";
            const string origin = "https://example.com";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = origin;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);
            formConfig.PrivacyMode = true;
            formConfig.Connectors =
            [
                new("mailjet", "email", true, new Dictionary<string, object>())
            ];

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);
            formConfigServiceMock.Setup(x => x.IsDomainAllowedForFormAsync(formId, origin))
                                  .ReturnsAsync(true);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(formId);
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithPrivacyModeButNoEnabledConnectors_ShouldReturnFailure()
        {
            // Arrange
            const string formId = "invalid-privacy-form";
            const string origin = "https://example.com";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = origin;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);
            formConfig.PrivacyMode = true;
            formConfig.Connectors =
            [
                new("slack", "disabled", false, new Dictionary<string, object>())
            ];

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Privacy mode requires at least one enabled connector");
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithPrivacyModeButNullConnectors_ShouldReturnFailure()
        {
            // Arrange
            const string formId = "no-connectors-privacy-form";
            const string origin = "https://example.com";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = origin;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);
            formConfig.PrivacyMode = true;
            formConfig.Connectors = null; // No connectors at all

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Privacy mode requires at least one enabled connector");
        }

        [Fact]
        public async Task ValidateFormAndOriginAsync_WithNormalModeAndNullConnectors_ShouldSucceed()
        {
            // Arrange
            const string formId = "normal-form";
            const string origin = "https://example.com";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Origin"] = origin;

            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["example.com"]);
            formConfig.PrivacyMode = false;
            formConfig.Connectors = null; // No connectors - should be fine when privacy mode is off

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);
            formConfigServiceMock.Setup(x => x.IsDomainAllowedForFormAsync(formId, origin))
                                  .ReturnsAsync(true);

            // Act
            var result = await service.ValidateFormAndOriginAsync(formId, httpContext.Request);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(formId);
        }
    }
}
