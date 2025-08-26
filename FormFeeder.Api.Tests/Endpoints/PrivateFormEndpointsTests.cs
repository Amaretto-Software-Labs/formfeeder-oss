using System.Net;
using System.Text;
using System.Text.Json;

using FormFeeder.Api.Endpoints;
using FormFeeder.Api.Models;
using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FormFeeder.Api.Tests.Endpoints;

public sealed class PrivateFormEndpointsTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public PrivateFormEndpointsTests(TestWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        client = this.factory.CreateClient();
    }

    [Fact]
    public async Task CreatePrivateFormFromEmail_WithValidJsonRequest_ReturnsFormId()
    {
        // Arrange
        var email = "test@example.com";
        var expectedFormId = "prv-test123";

        var factory = this.factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var mockFormGenService = new Mock<IPrivateFormGenerationService>();
                var mockFormConfig = new FormConfiguration
                {
                    FormId = expectedFormId,
                    PrivacyMode = true,
                    AllowedDomains = ["*"],
                    Enabled = true,
                };

                mockFormGenService
                    .Setup(x => x.GeneratePrivateFormAsync(email))
                    .ReturnsAsync(mockFormConfig);

                var mockFormConfigManagement = new Mock<IFormConfigurationManagementService>();
                mockFormConfigManagement
                    .Setup(x => x.CreateFormConfigurationAsync(It.IsAny<FormConfiguration>()))
                    .ReturnsAsync(mockFormConfig);

                services.AddScoped(_ => mockFormGenService.Object);
                services.AddScoped(_ => mockFormConfigManagement.Object);
            });
        });

        var client = factory.CreateClient();
        var jsonContent = JsonSerializer.Serialize(new { email });
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/forms/from-email", httpContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreatePrivateFormResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        result.Should().NotBeNull();
        result!.FormId.Should().Be(expectedFormId);
    }

    [Fact]
    public async Task CreatePrivateFormFromEmail_WithValidFormDataRequest_ReturnsFormId()
    {
        // Arrange
        var email = "test@example.com";
        var expectedFormId = "prv-test123";

        var factory = this.factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var mockFormGenService = new Mock<IPrivateFormGenerationService>();
                var mockFormConfig = new FormConfiguration
                {
                    FormId = expectedFormId,
                    PrivacyMode = true,
                    AllowedDomains = ["*"],
                    Enabled = true,
                };

                mockFormGenService
                    .Setup(x => x.GeneratePrivateFormAsync(email))
                    .ReturnsAsync(mockFormConfig);

                var mockFormConfigManagement = new Mock<IFormConfigurationManagementService>();
                mockFormConfigManagement
                    .Setup(x => x.CreateFormConfigurationAsync(It.IsAny<FormConfiguration>()))
                    .ReturnsAsync(mockFormConfig);

                services.AddScoped(_ => mockFormGenService.Object);
                services.AddScoped(_ => mockFormConfigManagement.Object);
            });
        });

        var client = factory.CreateClient();
        var formData = new List<KeyValuePair<string, string>>
        {
            new("email", email),
        };
        var httpContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await client.PostAsync("/v1/forms/from-email", httpContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreatePrivateFormResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        result.Should().NotBeNull();
        result!.FormId.Should().Be(expectedFormId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public async Task CreatePrivateFormFromEmail_WithInvalidEmail_ReturnsBadRequest(string invalidEmail)
    {
        // Arrange
        var jsonContent = JsonSerializer.Serialize(new { email = invalidEmail });
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/forms/from-email", httpContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePrivateFormFromEmail_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var jsonContent = JsonSerializer.Serialize(new { name = "test" });
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/forms/from-email", httpContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePrivateFormFromEmail_WithEmptyBody_ReturnsBadRequest()
    {
        // Arrange
        var httpContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/forms/from-email", httpContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePrivateFormFromEmail_WithInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var httpContent = new StringContent("{invalid json", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/v1/forms/from-email", httpContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePrivateFormFromEmail_WithUnsupportedContentType_ReturnsBadRequest()
    {
        // Arrange
        var httpContent = new StringContent("email=test@example.com", Encoding.UTF8, "text/plain");

        // Act
        var response = await client.PostAsync("/v1/forms/from-email", httpContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
