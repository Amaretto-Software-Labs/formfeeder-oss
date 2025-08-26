using System.Text;

using FormFeeder.Api.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace FormFeeder.Api.Tests.Services;

public class FormDataExtractionServiceTests
{
    private readonly FormDataExtractionService service;

    public FormDataExtractionServiceTests()
    {
        service = new FormDataExtractionService();
    }

    public class ExtractFormData : FormDataExtractionServiceTests
    {
        [Fact]
        public async Task ExtractFormData_WithFormContentType_ShouldExtractFields()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";

            var formCollection = new FormCollection(new Dictionary<string, StringValues>
            {
                ["name"] = "John Doe",
                ["email"] = "john@example.com",
                ["age"] = "30",
            });

            httpContext.Request.Form = formCollection;

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().HaveCount(3);
            result["name"].Should().Be("John Doe");
            result["email"].Should().Be("john@example.com");
            result["age"].Should().Be("30");
        }

        [Fact]
        public async Task ExtractFormData_WithMultipleValues_ShouldReturnArray()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";

            var formCollection = new FormCollection(new Dictionary<string, StringValues>
            {
                ["hobbies"] = new StringValues(["reading", "swimming", "coding"]),
            });

            httpContext.Request.Form = formCollection;

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().ContainKey("hobbies");
            result["hobbies"].Should().BeOfType<string[]>();
            var hobbies = result["hobbies"] as string[];
            hobbies.Should().Equal("reading", "swimming", "coding");
        }

        [Fact]
        public async Task ExtractFormData_WithEmptyValue_ShouldReturnEmptyString()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";

            var formCollection = new FormCollection(new Dictionary<string, StringValues>
            {
                ["empty_field"] = StringValues.Empty,
            });

            httpContext.Request.Form = formCollection;

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().ContainKey("empty_field");
            result["empty_field"].Should().Be(string.Empty);
        }

        [Fact]
        public async Task ExtractFormData_WithoutFormContentType_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractFormData_WithNullContentType_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractFormData_WithFiles_ShouldIncludeFileMetadata()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "multipart/form-data";

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Name).Returns("upload");
            mockFile.Setup(f => f.FileName).Returns("test.txt");
            mockFile.Setup(f => f.ContentType).Returns("text/plain");
            mockFile.Setup(f => f.Length).Returns(1024);

            var formCollection = new FormCollection(
                new Dictionary<string, StringValues> { ["name"] = "John" },
                new FormFileCollection { mockFile.Object });

            httpContext.Request.Form = formCollection;

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().ContainKey("name");
            result.Should().ContainKey("_files");

            var files = result["_files"] as object[];
            files.Should().NotBeNull();
            files.Should().HaveCount(1);
        }

        [Fact]
        public async Task ExtractFormData_WithMultipleFiles_ShouldIncludeAllFiles()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "multipart/form-data";

            var mockFile1 = new Mock<IFormFile>();
            mockFile1.Setup(f => f.Name).Returns("file1");
            mockFile1.Setup(f => f.FileName).Returns("doc1.pdf");
            mockFile1.Setup(f => f.ContentType).Returns("application/pdf");
            mockFile1.Setup(f => f.Length).Returns(2048);

            var mockFile2 = new Mock<IFormFile>();
            mockFile2.Setup(f => f.Name).Returns("file2");
            mockFile2.Setup(f => f.FileName).Returns("image.jpg");
            mockFile2.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile2.Setup(f => f.Length).Returns(4096);

            var formCollection = new FormCollection(
                new Dictionary<string, StringValues>(),
                new FormFileCollection { mockFile1.Object, mockFile2.Object });

            httpContext.Request.Form = formCollection;

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().ContainKey("_files");
            var files = result["_files"] as object[];
            files.Should().HaveCount(2);
        }

        [Theory]
        [InlineData("application/x-www-form-urlencoded")]
        [InlineData("multipart/form-data")]
        [InlineData("multipart/form-data; boundary=something")]
        public async Task ExtractFormData_WithDifferentFormContentTypes_ShouldProcessForm(string contentType)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = contentType;

            var formCollection = new FormCollection(new Dictionary<string, StringValues>
            {
                ["test"] = "value",
            });

            httpContext.Request.Form = formCollection;

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().ContainKey("test");
        }

        [Fact]
        public async Task ExtractFormData_WithSpecialCharactersInFieldNames_ShouldPreserve()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";

            var formCollection = new FormCollection(new Dictionary<string, StringValues>
            {
                ["field_with_underscore"] = "value1",
                ["field-with-dash"] = "value2",
                ["field[with][brackets]"] = "value3",
                ["field.with.dots"] = "value4",
            });

            httpContext.Request.Form = formCollection;

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().HaveCount(4);
            result["field_with_underscore"].Should().Be("value1");
            result["field-with-dash"].Should().Be("value2");
            result["field[with][brackets]"].Should().Be("value3");
            result["field.with.dots"].Should().Be("value4");
        }

        [Fact]
        public async Task ExtractFormData_WithJsonContentType_ShouldExtractFields()
        {
            // Arrange
            var json = """{"name": "John Doe", "email": "john@example.com", "age": 30}""";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().HaveCount(3);
            result["name"].Should().Be("John Doe");
            result["email"].Should().Be("john@example.com");
            result["age"].Should().Be(30);
        }

        [Fact]
        public async Task ExtractFormData_WithNestedJsonObject_ShouldExtractNestedFields()
        {
            // Arrange
            var json = """
            {
                "name": "John Doe",
                "address": {
                    "street": "123 Main St",
                    "city": "New York"
                }
            }
            """;
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().HaveCount(2);
            result["name"].Should().Be("John Doe");
            result["address"].Should().BeOfType<Dictionary<string, object>>();

            var address = (Dictionary<string, object>)result["address"];
            address["street"].Should().Be("123 Main St");
            address["city"].Should().Be("New York");
        }

        [Fact]
        public async Task ExtractFormData_WithJsonArray_ShouldReturnArray()
        {
            // Arrange
            var json = """{"hobbies": ["reading", "swimming", "coding"]}""";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().ContainKey("hobbies");
            result["hobbies"].Should().BeOfType<object[]>();
            var hobbies = (object[])result["hobbies"];
            hobbies.Should().Equal("reading", "swimming", "coding");
        }

        [Fact]
        public async Task ExtractFormData_WithMalformedJson_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var malformedJson = """{"name": "John", "email":}"""; // Missing value
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(malformedJson));

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractFormData_WithEmptyJsonObject_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var json = "{}";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractFormData_WithEmptyJsonBody_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExtractFormData_WithJsonContentTypeAndCharset_ShouldExtractFields()
        {
            // Arrange
            var json = """{"name": "John Doe", "email": "john@example.com"}""";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json; charset=utf-8";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            var result = await service.ExtractFormDataAsync(httpContext.Request);

            // Assert
            result.Should().HaveCount(2);
            result["name"].Should().Be("John Doe");
            result["email"].Should().Be("john@example.com");
        }
    }

    public class ExtractClientInfo : FormDataExtractionServiceTests
    {
        [Fact]
        public void ExtractClientInfo_WithAllHeaders_ShouldExtractAllInfo()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0";
            httpContext.Request.Headers["Referer"] = "https://example.com";
            httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.1";
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";

            // Act
            var result = service.ExtractClientInfo(httpContext.Request);

            // Assert
            result.IpAddress.Should().Be("203.0.113.1");
            result.UserAgent.Should().Be("Mozilla/5.0");
            result.Referer.Should().Be("https://example.com");
            result.ContentType.Should().Be("application/x-www-form-urlencoded");
        }

        [Fact]
        public void ExtractClientInfo_WithMissingHeaders_ShouldHandleGracefully()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            var result = service.ExtractClientInfo(httpContext.Request);

            // Assert
            result.IpAddress.Should().BeNull();
            result.UserAgent.Should().BeEmpty();
            result.Referer.Should().BeEmpty();
            result.ContentType.Should().BeNull();
        }

        [Fact]
        public void ExtractClientInfo_WithEmptyHeaders_ShouldReturnEmptyStrings()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["User-Agent"] = string.Empty;
            httpContext.Request.Headers["Referer"] = string.Empty;

            // Act
            var result = service.ExtractClientInfo(httpContext.Request);

            // Assert
            result.UserAgent.Should().Be(string.Empty);
            result.Referer.Should().Be(string.Empty);
        }

        [Fact]
        public void ExtractClientInfo_WithComplexUserAgent_ShouldPreserveComplete()
        {
            // Arrange
            const string complexUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["User-Agent"] = complexUserAgent;

            // Act
            var result = service.ExtractClientInfo(httpContext.Request);

            // Assert
            result.UserAgent.Should().Be(complexUserAgent);
        }

        [Fact]
        public void ExtractClientInfo_WithComplexReferer_ShouldPreserveComplete()
        {
            // Arrange
            const string complexReferer = "https://example.com/path/to/page?param1=value1&param2=value2#section";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Referer"] = complexReferer;

            // Act
            var result = service.ExtractClientInfo(httpContext.Request);

            // Assert
            result.Referer.Should().Be(complexReferer);
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("application/x-www-form-urlencoded")]
        [InlineData("multipart/form-data; boundary=something")]
        [InlineData("text/plain")]
        public void ExtractClientInfo_WithDifferentContentTypes_ShouldExtractCorrectly(string contentType)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = contentType;

            // Act
            var result = service.ExtractClientInfo(httpContext.Request);

            // Assert
            result.ContentType.Should().Be(contentType);
        }

        [Fact]
        public void ExtractClientInfo_WithXRealIpHeader_ShouldUseXRealIp()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Real-IP"] = "192.168.1.1";

            // Act
            var result = service.ExtractClientInfo(httpContext.Request);

            // Assert
            result.IpAddress.Should().Be("192.168.1.1");
        }
    }

    public class ClientInfoRecord : FormDataExtractionServiceTests
    {
        [Fact]
        public void ClientInfo_ShouldBeRecord()
        {
            // Arrange
            const string ip = "192.168.1.1";
            const string userAgent = "Mozilla/5.0";
            const string referer = "https://example.com";
            const string contentType = "application/json";

            // Act
            var info1 = new ClientInfo(ip, userAgent, referer, contentType);
            var info2 = new ClientInfo(ip, userAgent, referer, contentType);

            // Assert
            info1.Should().Be(info2); // Records have value equality
            info1.Should().NotBeSameAs(info2);
        }

        [Fact]
        public void ClientInfo_WithDifferentValues_ShouldNotBeEqual()
        {
            // Arrange
            var info1 = new ClientInfo("192.168.1.1", "Mozilla/5.0", "https://example.com", "application/json");
            var info2 = new ClientInfo("10.0.0.1", "Mozilla/5.0", "https://example.com", "application/json");

            // Assert
            info1.Should().NotBe(info2);
        }

        [Fact]
        public void ClientInfo_WithNullValues_ShouldBeValid()
        {
            // Act
            var info = new ClientInfo(null, null, null, null);

            // Assert
            info.IpAddress.Should().BeNull();
            info.UserAgent.Should().BeNull();
            info.Referer.Should().BeNull();
            info.ContentType.Should().BeNull();
        }
    }
}
