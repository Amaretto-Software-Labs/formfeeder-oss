using System.Text.Json;

using FormFeeder.Api.Connectors;
using FormFeeder.Api.Models;
using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;

using Microsoft.Extensions.Logging;

namespace FormFeeder.Api.Tests.Services;

public class FormSubmissionServiceTests : TestBase
{
    private readonly FormSubmissionService service;
    private readonly Mock<ILogger<FormSubmissionService>> loggerMock;
    private readonly Mock<IFormConfigurationService> formConfigServiceMock;

    public FormSubmissionServiceTests()
    {
        loggerMock = CreateMock<ILogger<FormSubmissionService>>();
        formConfigServiceMock = CreateMock<IFormConfigurationService>();
        service = new FormSubmissionService(DbContext, formConfigServiceMock.Object, loggerMock.Object);
        EnsureDatabaseCreated();
    }

    public class SaveSubmissionAsync : FormSubmissionServiceTests
    {
        [Fact]
        public async Task SaveSubmissionAsync_WithValidData_ShouldSaveSuccessfully()
        {
            // Arrange
            const string formId = "test-form";
            var formData = new Dictionary<string, object>
            {
                ["name"] = "John Doe",
                ["email"] = "john@example.com",
            };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = false,
                Enabled = true,
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(
                formId,
                formData,
                "192.168.1.1",
                "Mozilla/5.0",
                "https://example.com",
                "application/json");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Success.Should().BeTrue();
            result.Value.FormId.Should().Be(formId);
            result.Value.Message.Should().Be("Form submitted successfully");
            result.Value.Submission.Should().NotBeNull();

            // Verify database
            var savedSubmission = await DbContext.FormSubmissions.FindAsync(result.Value.Id);
            savedSubmission.Should().NotBeNull();
            savedSubmission!.FormId.Should().Be(formId);
            savedSubmission.ClientIp.Should().Be("192.168.1.1");
            savedSubmission.UserAgent.Should().Be("Mozilla/5.0");
            savedSubmission.Referer.Should().Be("https://example.com");
            savedSubmission.ContentType.Should().Be("application/json");
        }

        [Fact]
        public async Task SaveSubmissionAsync_WithComplexFormData_ShouldSerializeCorrectly()
        {
            // Arrange
            const string formId = "complex-form";
            var formData = new Dictionary<string, object>
            {
                ["user"] = new Dictionary<string, object>
                {
                    ["name"] = "John Doe",
                    ["age"] = 30,
                    ["preferences"] = new List<string> { "coffee", "tea" },
                },
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["active"] = true,
            };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = false,
                Enabled = true,
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(formId, formData, null, null, null, null);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Verify JSON structure is preserved
            var savedSubmission = await DbContext.FormSubmissions.FindAsync(result.Value!.Id);
            var formDataJson = savedSubmission!.FormData.RootElement;

            formDataJson.GetProperty("user").GetProperty("name").GetString().Should().Be("John Doe");
            formDataJson.GetProperty("user").GetProperty("age").GetInt32().Should().Be(30);
            formDataJson.GetProperty("active").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task SaveSubmissionAsync_WithEmptyFormData_ShouldSave()
        {
            // Arrange
            const string formId = "empty-form";
            var formData = new Dictionary<string, object>();

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = false,
                Enabled = true,
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(formId, formData, null, null, null, null);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var savedSubmission = await DbContext.FormSubmissions.FindAsync(result.Value!.Id);
            var formDataJson = savedSubmission!.FormData.RootElement;
            formDataJson.ValueKind.Should().Be(JsonValueKind.Object);
            formDataJson.EnumerateObject().Should().BeEmpty();
        }

        [Fact]
        public async Task SaveSubmissionAsync_WithNullOptionalParameters_ShouldSaveWithNulls()
        {
            // Arrange
            const string formId = "test-form";
            var formData = new Dictionary<string, object> { ["test"] = "value" };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = false,
                Enabled = true,
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(
                formId,
                formData,
                clientIp: null,
                userAgent: null,
                referer: null,
                contentType: null);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var savedSubmission = await DbContext.FormSubmissions.FindAsync(result.Value!.Id);
            savedSubmission!.ClientIp.Should().BeNull();
            savedSubmission.UserAgent.Should().BeNull();
            savedSubmission.Referer.Should().BeNull();
            savedSubmission.ContentType.Should().BeNull();
        }

        [Fact]
        public async Task SaveSubmissionAsync_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            const string formId = "special-form";
            var formData = new Dictionary<string, object>
            {
                ["unicode"] = "🚀 Hello 世界",
                ["special_chars"] = "<script>alert('xss')</script>",
                ["quotes"] = "He said \"Hello\" and she said 'Hi'",
                ["newlines"] = "Line 1\nLine 2\r\nLine 3",
            };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = false,
                Enabled = true,
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(formId, formData, null, null, null, null);

            // Assert
            result.IsSuccess.Should().BeTrue();

            var savedSubmission = await DbContext.FormSubmissions.FindAsync(result.Value!.Id);
            var formDataJson = savedSubmission!.FormData.RootElement;

            formDataJson.GetProperty("unicode").GetString().Should().Be("🚀 Hello 世界");
            formDataJson.GetProperty("special_chars").GetString().Should().Be("<script>alert('xss')</script>");
        }

        [Fact]
        public async Task SaveSubmissionAsync_ShouldSetSubmittedAtToCurrentTime()
        {
            // Arrange
            var before = DateTime.UtcNow.AddSeconds(-1);
            const string formId = "time-test";
            var formData = new Dictionary<string, object> { ["test"] = "value" };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = false,
                Enabled = true,
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(formId, formData, null, null, null, null);
            var after = DateTime.UtcNow.AddSeconds(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.SubmittedAt.Should().BeAfter(before).And.BeBefore(after);

            var savedSubmission = await DbContext.FormSubmissions.FindAsync(result.Value.Id);
            savedSubmission!.SubmittedAt.Should().BeAfter(before).And.BeBefore(after);
        }

        [Fact]
        public async Task SaveSubmissionAsync_ShouldGenerateUniqueIds()
        {
            // Arrange
            const string formId = "unique-test";
            var formData = new Dictionary<string, object> { ["test"] = "value" };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = false,
                Enabled = true,
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result1 = await service.SaveSubmissionAsync(formId, formData, null, null, null, null);
            var result2 = await service.SaveSubmissionAsync(formId, formData, null, null, null, null);

            // Assert
            result1.IsSuccess.Should().BeTrue();
            result2.IsSuccess.Should().BeTrue();
            result1.Value!.Id.Should().NotBe(result2.Value!.Id);
        }
    }

    public class PrivacyMode : FormSubmissionServiceTests
    {
        [Fact]
        public async Task SaveSubmissionAsync_WithPrivacyModeEnabled_ShouldReturnSuccessWithoutPersistingToDatabase()
        {
            // Arrange
            const string formId = "privacy-form";
            var formData = new Dictionary<string, object>
            {
                ["name"] = "John Doe",
                ["email"] = "john@example.com",
            };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = true,
                Enabled = true,
                Connectors = new List<ConnectorConfiguration>
                {
                    new("mailjet", "test", true, new Dictionary<string, object>())
                },
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(
                formId,
                formData,
                "192.168.1.1",
                "Mozilla/5.0",
                "https://example.com",
                "application/json");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Success.Should().BeTrue();
            result.Value.FormId.Should().Be(formId);
            result.Value.Message.Should().Be("Form submitted successfully");
            result.Value.Submission.Should().NotBeNull();

            // Verify submission was NOT saved to database (primary business behavior)
            var savedSubmission = await DbContext.FormSubmissions.FindAsync(result.Value.Id);
            savedSubmission.Should().BeNull("submission should not be persisted in privacy mode");
        }

        [Fact]
        public async Task SaveSubmissionAsync_WithPrivacyModeDisabled_ShouldPersistSubmissionToDatabase()
        {
            // Arrange
            const string formId = "normal-form";
            var formData = new Dictionary<string, object>
            {
                ["name"] = "Jane Doe",
                ["email"] = "jane@example.com",
            };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = false,
                Enabled = true,
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(
                formId,
                formData,
                "192.168.1.1",
                "Mozilla/5.0",
                "https://example.com",
                "application/json");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Success.Should().BeTrue();

            // Verify submission WAS saved to database (primary business behavior)
            var savedSubmission = await DbContext.FormSubmissions.FindAsync(result.Value.Id);
            savedSubmission.Should().NotBeNull("submission should be persisted when privacy mode is disabled");
            savedSubmission!.FormId.Should().Be(formId);
            savedSubmission.ClientIp.Should().Be("192.168.1.1");
        }

        [Fact]
        public async Task SaveSubmissionAsync_WithNonExistentForm_ShouldReturnFailureAndNotPersist()
        {
            // Arrange
            const string formId = "nonexistent-form";
            var formData = new Dictionary<string, object>
            {
                ["name"] = "Test User",
                ["email"] = "test@example.com",
            };

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync((FormConfiguration?)null);

            // Act
            var result = await service.SaveSubmissionAsync(
                formId,
                formData,
                "192.168.1.1",
                "Mozilla/5.0",
                "https://example.com",
                "application/json");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Form configuration not found");

            // Verify no submissions were saved
            var submissionCount = DbContext.FormSubmissions.Count();
            submissionCount.Should().Be(0, "no submissions should be saved for non-existent forms");
        }

        [Fact]
        public async Task SaveSubmissionAsync_WithDisabledForm_ShouldReturnFailureAndNotPersist()
        {
            // Arrange
            const string formId = "disabled-form";
            var formData = new Dictionary<string, object>
            {
                ["message"] = "This should not be saved",
                ["email"] = "test@example.com",
            };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = false,
                Enabled = false, // Form is disabled
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(
                formId,
                formData,
                "192.168.1.1",
                "Mozilla/5.0",
                "https://example.com",
                "application/json");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Form is currently disabled");

            // Verify no submissions were saved
            var submissionCount = DbContext.FormSubmissions.Count();
            submissionCount.Should().Be(0, "no submissions should be saved for disabled forms");
        }

        [Fact]
        public async Task SaveSubmissionAsync_WithInvalidPrivacyModeConfiguration_ShouldReturnFailure()
        {
            // Arrange
            const string formId = "invalid-privacy-form";
            var formData = new Dictionary<string, object>
            {
                ["message"] = "This should not be saved",
                ["email"] = "test@example.com",
            };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = true,  // Privacy mode enabled
                Enabled = true,
                Connectors = [], // But no connectors configured (invalid)
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(
                formId,
                formData,
                "192.168.1.1",
                "Mozilla/5.0",
                "https://example.com",
                "application/json");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid form configuration");

            // Verify no submissions were saved
            var submissionCount = DbContext.FormSubmissions.Count();
            submissionCount.Should().Be(0, "no submissions should be saved for invalid forms");
        }

        [Fact]
        public async Task SaveSubmissionAsync_PrivacyMode_ShouldStillReturnCompleteResponse()
        {
            // Arrange
            const string formId = "privacy-response-test";
            var formData = new Dictionary<string, object>
            {
                ["message"] = "This is a privacy mode test",
                ["priority"] = "high",
            };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = true,
                Enabled = true,
                Connectors = new List<ConnectorConfiguration>
                {
                    new("slack", "test", true, new Dictionary<string, object>())
                },
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            // Act
            var result = await service.SaveSubmissionAsync(
                formId,
                formData,
                "10.0.0.1",
                "Chrome/119.0",
                "https://testsite.com",
                "multipart/form-data");

            // Assert
            result.IsSuccess.Should().BeTrue();

            var response = result.Value!;
            response.Success.Should().BeTrue();
            response.FormId.Should().Be(formId);
            response.Message.Should().Be("Form submitted successfully");
            response.Id.Should().NotBe(Guid.Empty);
            response.Submission.Should().NotBeNull();

            // Verify the submission object contains the expected data
            var submission = response.Submission!;
            submission.FormId.Should().Be(formId);
            submission.ClientIp.Should().Be("10.0.0.1");
            submission.UserAgent.Should().Be("Chrome/119.0");
            submission.Referer.Should().Be("https://testsite.com");
            submission.ContentType.Should().Be("multipart/form-data");
            submission.FormData.RootElement.GetProperty("message").GetString().Should().Be("This is a privacy mode test");
            submission.FormData.RootElement.GetProperty("priority").GetString().Should().Be("high");
        }
    }

    public class ErrorHandling : FormSubmissionServiceTests
    {
        [Fact]
        public async Task SaveSubmissionAsync_WhenDbContextThrows_ShouldReturnFailure()
        {
            // Arrange
            const string formId = "error-test";
            var formData = new Dictionary<string, object> { ["test"] = "value" };

            var formConfig = new FormConfiguration
            {
                FormId = formId,
                PrivacyMode = false,
                Enabled = true,
            };
            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                .ReturnsAsync(formConfig);

            DbContext.Dispose(); // Force the context to be disposed to cause an error

            // Act
            var result = await service.SaveSubmissionAsync(formId, formData, null, null, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("An error occurred while processing your submission");
            result.Exception.Should().NotBeNull();
        }
    }
}
