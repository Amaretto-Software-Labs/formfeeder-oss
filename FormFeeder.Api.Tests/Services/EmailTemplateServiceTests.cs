using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;
using System.Text.Json;

namespace FormFeeder.Api.Tests.Services;

public class EmailTemplateServiceTests
{
    private readonly EmailTemplateService _service;

    public EmailTemplateServiceTests()
    {
        _service = new EmailTemplateService();
    }

    public class GenerateEmailContent : EmailTemplateServiceTests
    {
        [Fact]
        public void GenerateEmailContent_WithCompleteSubmission_ShouldGenerateValidContent()
        {
            // Arrange
            var formData = new Dictionary<string, object>
            {
                ["name"] = "John Doe",
                ["email"] = "john@example.com",
                ["message"] = "This is a test message"
            };
            var submission = TestDataBuilder.CreateFormSubmission(
                formId: "contact-form",
                formData: formData,
                submittedAt: new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                clientIp: "192.168.1.1",
                userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                referer: "https://example.com/contact");

            // Act
            var result = _service.GenerateEmailContent(submission);

            // Assert
            result.Should().NotBeNull();
            result.Html.Should().NotBeNullOrEmpty();
            result.Text.Should().NotBeNullOrEmpty();

            // Verify HTML content
            result.Html.Should().Contain("New Form Submission: contact-form");
            result.Html.Should().Contain("2025-01-15 10:30:00 UTC");
            result.Html.Should().Contain("192.168.1.1");
            result.Html.Should().Contain("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            result.Html.Should().Contain("https://example.com/contact");
            result.Html.Should().Contain("John Doe");
            result.Html.Should().Contain("john@example.com");
            result.Html.Should().Contain("This is a test message");

            // Verify Text content
            result.Text.Should().Contain("New Form Submission: contact-form");
            result.Text.Should().Contain("2025-01-15 10:30:00 UTC");
            result.Text.Should().Contain("192.168.1.1");
            result.Text.Should().Contain("John Doe");
            result.Text.Should().Contain("john@example.com");
            result.Text.Should().Contain("This is a test message");
        }

        [Fact]
        public void GenerateEmailContent_WithNullOptionalFields_ShouldShowUnknown()
        {
            // Arrange
            var formData = new Dictionary<string, object> { ["test"] = "value" };
            var submission = TestDataBuilder.CreateFormSubmission(
                formData: formData,
                clientIp: null,
                userAgent: null,
                referer: null);

            // Act
            var result = _service.GenerateEmailContent(submission);

            // Assert
            result.Html.Should().Contain("Unknown")
                      .And.NotContain("null");
            result.Text.Should().Contain("Unknown")
                      .And.NotContain("null");
        }

        [Fact]
        public void GenerateEmailContent_WithEmptyFormData_ShouldHandleGracefully()
        {
            // Arrange
            var emptyData = new Dictionary<string, object>();
            var submission = TestDataBuilder.CreateFormSubmission(formData: emptyData);

            // Act
            var result = _service.GenerateEmailContent(submission);

            // Assert
            result.Html.Should().Contain("No form data submitted");
            result.Text.Should().Contain("No form data submitted");
        }

        [Fact]
        public void GenerateEmailContent_WithComplexFormData_ShouldSerializeCorrectly()
        {
            // Arrange
            var complexData = new Dictionary<string, object>
            {
                ["user"] = new Dictionary<string, object>
                {
                    ["name"] = "Jane Smith",
                    ["age"] = 25
                },
                ["preferences"] = new List<string> { "coffee", "tea" },
                ["active"] = true,
                ["score"] = 95.5
            };
            var submission = TestDataBuilder.CreateFormSubmission(formData: complexData);

            // Act
            var result = _service.GenerateEmailContent(submission);

            // Assert
            // The JsonElement.ToString() will serialize the complex objects
            result.Html.Should().Contain("user").And.Contain("preferences").And.Contain("active").And.Contain("score");
            result.Text.Should().Contain("user").And.Contain("preferences").And.Contain("active").And.Contain("score");
        }

        [Fact]
        public void GenerateEmailContent_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var specialData = new Dictionary<string, object>
            {
                ["unicode"] = "🚀 Hello 世界",
                ["html"] = "<script>alert('test')</script>",
                ["quotes"] = "He said \"Hello\" and she said 'Hi'",
                ["newlines"] = "Line 1\nLine 2"
            };
            var submission = TestDataBuilder.CreateFormSubmission(formData: specialData);

            // Act
            var result = _service.GenerateEmailContent(submission);

            // Assert
            result.Html.Should().Contain("🚀 Hello 世界");
            result.Text.Should().Contain("🚀 Hello 世界");
            
            // HTML should contain the script tags as text (not executed)
            result.Html.Should().Contain("<script>alert('test')</script>");
        }

        [Fact]
        public void GenerateEmailContent_ShouldProduceValidHtmlStructure()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();

            // Act
            var result = _service.GenerateEmailContent(submission);

            // Assert
            result.Html.Should().StartWith("<!DOCTYPE html>")
                       .And.Contain("<html>")
                       .And.Contain("</html>")
                       .And.Contain("<head>")
                       .And.Contain("</head>")
                       .And.Contain("<body>")
                       .And.Contain("</body>")
                       .And.Contain("<style>");
        }

        [Fact]
        public void GenerateEmailContent_ShouldIncludeBasicStyling()
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission();

            // Act
            var result = _service.GenerateEmailContent(submission);

            // Assert
            result.Html.Should().Contain("font-family: Arial")
                       .And.Contain("background:")
                       .And.Contain("padding:")
                       .And.Contain("margin:")
                       .And.Contain(".container")
                       .And.Contain(".header")
                       .And.Contain(".metadata")
                       .And.Contain(".form-data");
        }

        [Fact]
        public void GenerateEmailContent_WithLongFormId_ShouldIncludeInBothFormats()
        {
            // Arrange
            const string longFormId = "very-long-form-identifier-that-exceeds-normal-length-expectations-and-continues-for-quite-some-time";
            var submission = TestDataBuilder.CreateFormSubmission(formId: longFormId);

            // Act
            var result = _service.GenerateEmailContent(submission);

            // Assert
            result.Html.Should().Contain(longFormId);
            result.Text.Should().Contain(longFormId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GenerateEmailContent_WithEmptyOrWhitespaceFormId_ShouldHandleGracefully(string formId)
        {
            // Arrange
            var submission = TestDataBuilder.CreateFormSubmission(formId: formId);

            // Act
            var result = _service.GenerateEmailContent(submission);

            // Assert
            result.Html.Should().NotBeNullOrEmpty();
            result.Text.Should().NotBeNullOrEmpty();
            result.Html.Should().Contain($"New Form Submission: {formId}");
            result.Text.Should().Contain($"New Form Submission: {formId}");
        }
    }

    public class EmailContentRecord : EmailTemplateServiceTests
    {
        [Fact]
        public void EmailContent_ShouldBeRecord()
        {
            // Arrange
            const string html = "<html>test</html>";
            const string text = "test";

            // Act
            var content1 = new EmailContent(html, text);
            var content2 = new EmailContent(html, text);

            // Assert
            content1.Should().Be(content2); // Records have value equality
            content1.Html.Should().Be(html);
            content1.Text.Should().Be(text);
        }

        [Fact]
        public void EmailContent_WithDifferentValues_ShouldNotBeEqual()
        {
            // Arrange
            var content1 = new EmailContent("<html>test1</html>", "test1");
            var content2 = new EmailContent("<html>test2</html>", "test2");

            // Assert
            content1.Should().NotBe(content2);
        }
    }
}