using System.Net;
using System.Text;
using System.Text.Json;

using FormFeeder.Api.Endpoints;
using FormFeeder.Api.Tests.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

namespace FormFeeder.Api.Tests.Integration;

public sealed class PrivateFormEndpointsIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public PrivateFormEndpointsIntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        client = this.factory.CreateClient();
    }

    [Fact]
    public async Task CreatePrivateFormFromEmail_EndToEnd_WithValidJsonRequest_CreatesFormSuccessfully()
    {
        // Arrange
        var email = "integration-test@example.com";
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
        result!.FormId.Should().StartWith("prv-");
        result.FormId.Length.Should().BeGreaterOrEqualTo(10);

        // Verify the form was actually created in the database
        using var scope = factory.Services.CreateScope();
        var formConfigService = scope.ServiceProvider.GetRequiredService<FormFeeder.Api.Services.IFormConfigurationService>();
        var createdForm = await formConfigService.GetFormConfigurationAsync(result.FormId);

        createdForm.Should().NotBeNull();
        createdForm!.FormId.Should().Be(result.FormId);
        createdForm.PrivacyMode.Should().BeTrue();
        createdForm.Enabled.Should().BeTrue();
        createdForm.AllowedDomains.Should().ContainSingle("*");
        createdForm.Description.Should().Contain(email);
        createdForm.Connectors.Should().ContainSingle();

        var connector = createdForm.Connectors!.Single();
        connector.Type.Should().Be("MailJet");
        connector.Enabled.Should().BeTrue();
        connector.Settings["ToEmail"].Should().Be(email);
    }

    [Fact]
    public async Task CreatePrivateFormFromEmail_EndToEnd_WithValidFormDataRequest_CreatesFormSuccessfully()
    {
        // Arrange
        var email = "integration-form-data@example.com";
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
        result!.FormId.Should().StartWith("prv-");

        // Verify the form was actually created in the database
        using var scope = factory.Services.CreateScope();
        var formConfigService = scope.ServiceProvider.GetRequiredService<FormFeeder.Api.Services.IFormConfigurationService>();
        var createdForm = await formConfigService.GetFormConfigurationAsync(result.FormId);

        createdForm.Should().NotBeNull();
        createdForm!.Connectors!.First().Settings["ToEmail"].Should().Be(email);
    }

    [Fact]
    public async Task CreatePrivateFormFromEmail_GeneratesUniqueFormIds()
    {
        // Arrange
        var email = "unique-test@example.com";
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Create multiple forms concurrently
        for (int i = 0; i < 5; i++)
        {
            var jsonContent = JsonSerializer.Serialize(new { email = $"{i}-{email}" });
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            tasks.Add(client.PostAsync("/v1/forms/from-email", httpContent));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        var formIds = new HashSet<string>();
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CreatePrivateFormResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            result.Should().NotBeNull();
            formIds.Add(result!.FormId);
        }

        formIds.Should().HaveCount(5, "all generated form IDs should be unique");
    }

    [Theory]
    [InlineData("valid@example.com")]
    [InlineData("user.name+tag@domain.co.uk")]
    [InlineData("test123@sub.domain.org")]
    public async Task CreatePrivateFormFromEmail_WithVariousValidEmails_Succeeds(string validEmail)
    {
        // Arrange
        var jsonContent = JsonSerializer.Serialize(new { email = validEmail });
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
        result!.FormId.Should().StartWith("prv-");

        // Verify configuration is correct
        using var scope = factory.Services.CreateScope();
        var formConfigService = scope.ServiceProvider.GetRequiredService<FormFeeder.Api.Services.IFormConfigurationService>();
        var createdForm = await formConfigService.GetFormConfigurationAsync(result.FormId);

        createdForm.Should().NotBeNull();
        createdForm!.Connectors!.First().Settings["ToEmail"].Should().Be(validEmail);
    }

    [Fact]
    public async Task CreatePrivateFormFromEmail_WithRealMailJetConfiguration_ConfiguresConnectorCorrectly()
    {
        // Arrange
        var email = "mailjet-config-test@example.com";
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

        // Verify MailJet configuration from appsettings is applied
        using var scope = factory.Services.CreateScope();
        var formConfigService = scope.ServiceProvider.GetRequiredService<FormFeeder.Api.Services.IFormConfigurationService>();
        var createdForm = await formConfigService.GetFormConfigurationAsync(result!.FormId);

        var connector = createdForm!.Connectors!.Single();
        connector.Settings["ApiKey"].Should().Be("bd810bf147bc374e556a7e1d7c544a3b");
        connector.Settings["ApiSecret"].Should().Be("9af927ad95719b42dcae904905bb9b6c");
        connector.Settings["FromEmail"].Should().Be("no-reply@vasoftware.co.uk");
        connector.Settings["FromName"].Should().Be("FormFeeder");
        connector.Settings["Subject"].Should().Be("New Form Submission");
        connector.Settings["TemplateId"].Should().Be("7251954");
        connector.Settings["ToEmail"].Should().Be(email);
        connector.Settings["ToName"].Should().Be("Recipient");
    }
}
