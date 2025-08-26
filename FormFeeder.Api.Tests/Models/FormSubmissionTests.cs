using System.Text.Json;

using FormFeeder.Api.Models;
using FormFeeder.Api.Tests.Infrastructure;

namespace FormFeeder.Api.Tests.Models;

public class FormSubmissionTests
{
    public class Properties : FormSubmissionTests
    {
        [Fact]
        public void FormSubmission_ShouldHaveRequiredProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string formId = "test-form";
            var formData = JsonDocument.Parse("{\"name\":\"test\"}");
            var submittedAt = DateTime.UtcNow;
            const string clientIp = "192.168.1.1";
            const string userAgent = "Mozilla/5.0";
            const string referer = "https://example.com";
            const string contentType = "application/json";

            // Act
            var submission = new FormSubmission
            {
                Id = id,
                FormId = formId,
                FormData = formData,
                SubmittedAt = submittedAt,
                ClientIp = clientIp,
                UserAgent = userAgent,
                Referer = referer,
                ContentType = contentType,
            };

            // Assert
            submission.Id.Should().Be(id);
            submission.FormId.Should().Be(formId);
            submission.FormData.Should().Be(formData);
            submission.SubmittedAt.Should().Be(submittedAt);
            submission.ClientIp.Should().Be(clientIp);
            submission.UserAgent.Should().Be(userAgent);
            submission.Referer.Should().Be(referer);
            submission.ContentType.Should().Be(contentType);
        }

        [Fact]
        public void FormSubmission_OptionalProperties_ShouldAllowNull()
        {
            // Arrange & Act
            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = "test",
                FormData = JsonDocument.Parse("{}"),
                ClientIp = null,
                UserAgent = null,
                Referer = null,
                ContentType = null,
            };

            // Assert
            submission.ClientIp.Should().BeNull();
            submission.UserAgent.Should().BeNull();
            submission.Referer.Should().BeNull();
            submission.ContentType.Should().BeNull();
        }

        [Fact]
        public void FormSubmission_FormIdProperty_ShouldBeRequired()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = "test",
                FormData = JsonDocument.Parse("{}"),
            };

            // Act & Assert
            submission.FormId.Should().NotBeNull(); // Required property is initialized
        }

        [Fact]
        public void FormSubmission_FormDataProperty_ShouldBeRequired()
        {
            // Arrange
            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = "test",
                FormData = JsonDocument.Parse("{}"),
            };

            // Act & Assert
            submission.FormData.Should().NotBeNull(); // Required property is initialized
        }
    }

    public class JsonDocumentHandling : FormSubmissionTests
    {
        [Fact]
        public void FormSubmission_WithComplexJsonData_ShouldPreserveStructure()
        {
            // Arrange
            var complexJson = """
                {
                    "user": {
                        "name": "John Doe",
                        "age": 30,
                        "preferences": ["coffee", "tea"]
                    },
                    "timestamp": "2025-01-15T10:30:00Z",
                    "active": true,
                    "score": 95.5
                }
                """;

            var jsonDocument = JsonDocument.Parse(complexJson);

            // Act
            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = "complex-form",
                FormData = jsonDocument,
            };

            // Assert
            var rootElement = submission.FormData.RootElement;
            rootElement.GetProperty("user").GetProperty("name").GetString().Should().Be("John Doe");
            rootElement.GetProperty("user").GetProperty("age").GetInt32().Should().Be(30);
            rootElement.GetProperty("active").GetBoolean().Should().BeTrue();
            rootElement.GetProperty("score").GetDouble().Should().Be(95.5);
        }

        [Fact]
        public void FormSubmission_WithEmptyJsonData_ShouldHandleGracefully()
        {
            // Arrange
            var emptyJson = JsonDocument.Parse("{}");

            // Act
            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = "empty-form",
                FormData = emptyJson,
            };

            // Assert
            var rootElement = submission.FormData.RootElement;
            rootElement.ValueKind.Should().Be(JsonValueKind.Object);
            rootElement.EnumerateObject().Should().BeEmpty();
        }

        [Fact]
        public void FormSubmission_WithJsonArray_ShouldHandle()
        {
            // Arrange
            var arrayJson = JsonDocument.Parse("""["value1", "value2", "value3"]""");

            // Act
            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = "array-form",
                FormData = arrayJson,
            };

            // Assert
            var rootElement = submission.FormData.RootElement;
            rootElement.ValueKind.Should().Be(JsonValueKind.Array);
            rootElement.GetArrayLength().Should().Be(3);
        }

        [Fact]
        public void FormSubmission_WithSpecialCharactersInJson_ShouldPreserve()
        {
            // Arrange
            var specialJson = JsonDocument.Parse("""
                {
                    "unicode": "🚀 Hello 世界",
                    "html": "<script>alert('test')</script>",
                    "quotes": "He said \"Hello\" and she said 'Hi'",
                    "newlines": "Line 1\nLine 2\r\nLine 3"
                }
                """);

            // Act
            var submission = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = "special-form",
                FormData = specialJson,
            };

            // Assert
            var rootElement = submission.FormData.RootElement;
            rootElement.GetProperty("unicode").GetString().Should().Be("🚀 Hello 世界");
            rootElement.GetProperty("html").GetString().Should().Be("<script>alert('test')</script>");
            rootElement.GetProperty("quotes").GetString().Should().Be("He said \"Hello\" and she said 'Hi'");
            rootElement.GetProperty("newlines").GetString().Should().Be("Line 1\nLine 2\r\nLine 3");
        }
    }

    public class Equality : FormSubmissionTests
    {
        [Fact]
        public void FormSubmission_WithSameData_ShouldNotBeEqual()
        {
            // Arrange
            var id = Guid.NewGuid();
            var formData = JsonDocument.Parse("{\"test\": \"value\"}");

            var submission1 = new FormSubmission
            {
                Id = id,
                FormId = "test",
                FormData = formData,
            };

            var submission2 = new FormSubmission
            {
                Id = id,
                FormId = "test",
                FormData = formData,
            };

            // Assert
            // FormSubmission is a class, not a record, so instances are compared by reference
            submission1.Should().NotBe(submission2);
            submission1.Should().BeSameAs(submission1);
        }

        [Fact]
        public void FormSubmission_WithDifferentIds_ShouldHaveDifferentIds()
        {
            // Arrange
            var submission1 = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = "test",
                FormData = JsonDocument.Parse("{}"),
            };

            var submission2 = new FormSubmission
            {
                Id = Guid.NewGuid(),
                FormId = "test",
                FormData = JsonDocument.Parse("{}"),
            };

            // Assert
            submission1.Id.Should().NotBe(submission2.Id);
        }
    }

    public class DatabaseIntegration : FormSubmissionTests
    {
        [Fact]
        public void FormSubmission_ShouldHaveColumnTypeAttributeForFormData()
        {
            // Arrange & Act
            var property = typeof(FormSubmission).GetProperty(nameof(FormSubmission.FormData));
            var attribute = property?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute), false)
                                    .FirstOrDefault() as System.ComponentModel.DataAnnotations.Schema.ColumnAttribute;

            // Assert
            attribute.Should().NotBeNull();
            attribute!.TypeName.Should().Be("jsonb");
        }

        [Fact]
        public void FormSubmission_IdProperty_ShouldBeGuid()
        {
            // Arrange & Act
            var property = typeof(FormSubmission).GetProperty(nameof(FormSubmission.Id));

            // Assert
            property.Should().NotBeNull();
            property!.PropertyType.Should().Be(typeof(Guid));
        }

        [Fact]
        public void FormSubmission_SubmittedAtProperty_ShouldBeDateTime()
        {
            // Arrange & Act
            var property = typeof(FormSubmission).GetProperty(nameof(FormSubmission.SubmittedAt));

            // Assert
            property.Should().NotBeNull();
            property!.PropertyType.Should().Be(typeof(DateTime));
        }
    }

    public class Builder : FormSubmissionTests
    {
        [Fact]
        public void TestDataBuilder_CreateFormSubmission_ShouldCreateValidInstance()
        {
            // Act
            var submission = TestDataBuilder.CreateFormSubmission();

            // Assert
            submission.Should().NotBeNull();
            submission.Id.Should().NotBeEmpty();
            submission.FormId.Should().NotBeNullOrEmpty();
            submission.FormData.Should().NotBeNull();
            submission.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void TestDataBuilder_CreateFormSubmission_WithCustomValues_ShouldUseProvidedValues()
        {
            // Arrange
            const string customFormId = "custom-form";
            var customData = new Dictionary<string, object> { ["custom"] = "value" };
            var customDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            const string customIp = "10.0.0.1";

            // Act
            var submission = TestDataBuilder.CreateFormSubmission(
                formId: customFormId,
                formData: customData,
                submittedAt: customDate,
                clientIp: customIp);

            // Assert
            submission.FormId.Should().Be(customFormId);
            submission.SubmittedAt.Should().Be(customDate);
            submission.ClientIp.Should().Be(customIp);

            var formDataElement = submission.FormData.RootElement;
            formDataElement.GetProperty("custom").GetString().Should().Be("value");
        }
    }
}
