using FormFeeder.Api.Models;
using FormFeeder.Api.Models.DTOs;
using FormFeeder.Api.Tests.Infrastructure;

namespace FormFeeder.Api.Tests.Models.DTOs;

public class FormSubmissionResponseTests
{
    public class RecordBehavior : FormSubmissionResponseTests
    {
        [Fact]
        public void FormSubmissionResponse_ShouldBeRecord()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string formId = "test-form";
            var submittedAt = DateTime.UtcNow;
            const bool success = true;
            const string message = "Success";
            var submission = TestDataBuilder.CreateFormSubmission();

            // Act
            var response1 = new FormSubmissionResponse(id, formId, submittedAt, success, message, submission);
            var response2 = new FormSubmissionResponse(id, formId, submittedAt, success, message, submission);

            // Assert
            response1.Should().Be(response2); // Records have value equality
            response1.Should().NotBeSameAs(response2); // Different instances
        }

        [Fact]
        public void FormSubmissionResponse_WithDifferentValues_ShouldNotBeEqual()
        {
            // Arrange
            var baseData = (Guid.NewGuid(), "test-form", DateTime.UtcNow, true, "Success", TestDataBuilder.CreateFormSubmission());
            
            var response1 = new FormSubmissionResponse(baseData.Item1, baseData.Item2, baseData.Item3, baseData.Item4, baseData.Item5, baseData.Item6);
            var response2 = new FormSubmissionResponse(Guid.NewGuid(), baseData.Item2, baseData.Item3, baseData.Item4, baseData.Item5, baseData.Item6);

            // Assert
            response1.Should().NotBe(response2);
        }
    }

    public class Properties : FormSubmissionResponseTests
    {
        [Fact]
        public void FormSubmissionResponse_ShouldHaveRequiredProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string formId = "test-form";
            var submittedAt = DateTime.UtcNow;
            const bool success = true;
            const string message = "Success";
            var submission = TestDataBuilder.CreateFormSubmission();

            // Act
            var response = new FormSubmissionResponse(id, formId, submittedAt, success, message, submission);

            // Assert
            response.Id.Should().Be(id);
            response.FormId.Should().Be(formId);
            response.SubmittedAt.Should().Be(submittedAt);
            response.Success.Should().Be(success);
            response.Message.Should().Be(message);
            response.Submission.Should().Be(submission);
        }

        [Fact]
        public void FormSubmissionResponse_WithMinimalParameters_ShouldUseDefaults()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string formId = "test-form";
            var submittedAt = DateTime.UtcNow;
            const bool success = true;

            // Act
            var response = new FormSubmissionResponse(id, formId, submittedAt, success);

            // Assert
            response.Id.Should().Be(id);
            response.FormId.Should().Be(formId);
            response.SubmittedAt.Should().Be(submittedAt);
            response.Success.Should().Be(success);
            response.Message.Should().BeNull();
            response.Submission.Should().BeNull();
        }

        [Fact]
        public void FormSubmissionResponse_WithNullOptionalParameters_ShouldAcceptNulls()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string formId = "test-form";
            var submittedAt = DateTime.UtcNow;
            const bool success = false;

            // Act
            var response = new FormSubmissionResponse(id, formId, submittedAt, success, Message: null, Submission: null);

            // Assert
            response.Message.Should().BeNull();
            response.Submission.Should().BeNull();
        }
    }

    public class SuccessScenarios : FormSubmissionResponseTests
    {
        [Fact]
        public void FormSubmissionResponse_ForSuccessfulSubmission_ShouldHaveCorrectProperties()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();
            const string successMessage = "Form submitted successfully";

            // Act
            var response = new FormSubmissionResponse(
                submission.Id,
                submission.FormId ?? "test-form",
                submission.SubmittedAt,
                true,
                successMessage,
                submission);

            // Assert
            response.Success.Should().BeTrue();
            response.Message.Should().Be(successMessage);
            response.Submission.Should().Be(submission);
            response.Id.Should().Be(submission.Id);
            response.FormId.Should().Be(submission.FormId);
            response.SubmittedAt.Should().Be(submission.SubmittedAt);
        }

        [Fact]
        public void FormSubmissionResponse_ForSuccessWithoutSubmission_ShouldWork()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string formId = "test-form";
            var submittedAt = DateTime.UtcNow;
            const string message = "Success";

            // Act
            var response = new FormSubmissionResponse(id, formId, submittedAt, true, message);

            // Assert
            response.Success.Should().BeTrue();
            response.Message.Should().Be(message);
            response.Submission.Should().BeNull();
        }
    }

    public class ErrorScenarios : FormSubmissionResponseTests
    {
        [Fact]
        public void FormSubmissionResponse_ForFailedSubmission_ShouldHaveCorrectProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string formId = "test-form";
            var submittedAt = DateTime.UtcNow;
            const string errorMessage = "Validation failed";

            // Act
            var response = new FormSubmissionResponse(id, formId, submittedAt, false, errorMessage);

            // Assert
            response.Success.Should().BeFalse();
            response.Message.Should().Be(errorMessage);
            response.Submission.Should().BeNull();
        }

        [Fact]
        public void FormSubmissionResponse_ForErrorWithoutMessage_ShouldWork()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string formId = "test-form";
            var submittedAt = DateTime.UtcNow;

            // Act
            var response = new FormSubmissionResponse(id, formId, submittedAt, false);

            // Assert
            response.Success.Should().BeFalse();
            response.Message.Should().BeNull();
        }
    }

    public class EdgeCases : FormSubmissionResponseTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void FormSubmissionResponse_WithEmptyOrWhitespaceFormId_ShouldAccept(string formId)
        {
            // Act
            var response = new FormSubmissionResponse(Guid.NewGuid(), formId, DateTime.UtcNow, true);

            // Assert
            response.FormId.Should().Be(formId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void FormSubmissionResponse_WithEmptyOrWhitespaceMessage_ShouldAccept(string message)
        {
            // Act
            var response = new FormSubmissionResponse(Guid.NewGuid(), "test", DateTime.UtcNow, true, message);

            // Assert
            response.Message.Should().Be(message);
        }

        [Fact]
        public void FormSubmissionResponse_WithEmptyGuid_ShouldAccept()
        {
            // Act
            var response = new FormSubmissionResponse(Guid.Empty, "test", DateTime.UtcNow, true);

            // Assert
            response.Id.Should().Be(Guid.Empty);
        }

        [Fact]
        public void FormSubmissionResponse_WithMinDateTime_ShouldAccept()
        {
            // Act
            var response = new FormSubmissionResponse(Guid.NewGuid(), "test", DateTime.MinValue, true);

            // Assert
            response.SubmittedAt.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void FormSubmissionResponse_WithMaxDateTime_ShouldAccept()
        {
            // Act
            var response = new FormSubmissionResponse(Guid.NewGuid(), "test", DateTime.MaxValue, true);

            // Assert
            response.SubmittedAt.Should().Be(DateTime.MaxValue);
        }
    }

    public class Builder : FormSubmissionResponseTests
    {
        [Fact]
        public void TestDataBuilder_CreateFormSubmissionResponse_ShouldCreateValidInstance()
        {
            // Act
            var response = TestDataBuilder.CreateFormSubmissionResponse();

            // Assert
            response.Should().NotBeNull();
            response.Id.Should().NotBeEmpty();
            response.FormId.Should().NotBeNullOrEmpty();
            response.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            response.Success.Should().BeTrue();
            response.Message.Should().NotBeNullOrEmpty();
            response.Submission.Should().NotBeNull();
        }

        [Fact]
        public void TestDataBuilder_CreateFormSubmissionResponse_WithCustomValues_ShouldUseProvidedValues()
        {
            // Arrange
            var customId = Guid.NewGuid();
            const string customFormId = "custom-form";
            var customDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            const bool customSuccess = false;
            const string customMessage = "Custom error";

            // Act
            var response = TestDataBuilder.CreateFormSubmissionResponse(
                id: customId,
                formId: customFormId,
                submittedAt: customDate,
                success: customSuccess,
                message: customMessage);

            // Assert
            response.Id.Should().Be(customId);
            response.FormId.Should().Be(customFormId);
            response.SubmittedAt.Should().Be(customDate);
            response.Success.Should().Be(customSuccess);
            response.Message.Should().Be(customMessage);
        }
    }

    public class Immutability : FormSubmissionResponseTests
    {
        [Fact]
        public void FormSubmissionResponse_ShouldBeImmutable()
        {
            // Arrange
            var response = new FormSubmissionResponse(Guid.NewGuid(), "test", DateTime.UtcNow, true);

            // Act & Assert
            // Records are immutable by default - we can't change properties after creation
            // The compiler prevents modification of init-only properties
            response.Id.Should().NotBeEmpty();
            response.FormId.Should().Be("test");
        }

        [Fact]
        public void FormSubmissionResponse_WithCloning_ShouldCreateNewInstance()
        {
            // Arrange
            var original = new FormSubmissionResponse(Guid.NewGuid(), "test", DateTime.UtcNow, true, "Original");

            // Act
            var modified = original with { Message = "Modified" };

            // Assert
            modified.Should().NotBeSameAs(original);
            modified.Message.Should().Be("Modified");
            original.Message.Should().Be("Original");
            
            // Other properties should be the same
            modified.Id.Should().Be(original.Id);
            modified.FormId.Should().Be(original.FormId);
            modified.SubmittedAt.Should().Be(original.SubmittedAt);
            modified.Success.Should().Be(original.Success);
        }
    }
}