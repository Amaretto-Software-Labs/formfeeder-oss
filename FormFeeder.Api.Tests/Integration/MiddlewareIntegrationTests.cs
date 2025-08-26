using System.Net;
using System.Text;

using FormFeeder.Api.Middleware;
using FormFeeder.Api.Models;
using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FormFeeder.Api.Tests.Integration;

public class MiddlewareIntegrationTests
{
    public class DynamicCorsMiddlewareTests : IAsyncDisposable
    {
        private readonly IHost host;
        private readonly HttpClient client;
        private readonly Mock<IFormConfigurationService> formConfigServiceMock;
        private readonly IConfiguration testConfiguration;

        public DynamicCorsMiddlewareTests()
        {
            formConfigServiceMock = new Mock<IFormConfigurationService>();

            // Create test configuration with AllowedOrigins
            var configValues = new Dictionary<string, string?>
            {
                ["AllowedOrigins:0"] = "https://example.com",
                ["AllowedOrigins:1"] = "http://localhost:3000",
                ["AllowedOrigins:2"] = "https://subdomain.example.com",
            };

            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues);
            testConfiguration = configBuilder.Build();

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddSingleton(formConfigServiceMock.Object);
                        services.AddSingleton(testConfiguration);
                        services.AddLogging();
                    });
                    webHost.Configure(app =>
                    {
                        app.UseMiddleware<DynamicCorsMiddleware>();
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("OK");
                        });
                    });
                });

            host = hostBuilder.Build();
            host.Start();
            client = host.GetTestClient();
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithAllowedOrigin_ShouldSetCorsHeaders()
        {
            // Arrange
            const string origin = "https://example.com";
            client.DefaultRequestHeaders.Add("Origin", origin);

            // Act - Test generic endpoint (not form-specific)
            var response = await client.GetAsync("/");

            // Assert
            response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin" && h.Value.Contains(origin));
            response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Credentials" && h.Value.Contains("true"));
            response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Methods" && h.Value.Contains("GET, POST, PUT, DELETE, OPTIONS"));
            response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Headers" && h.Value.Contains("Content-Type, Authorization"));
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithWildcardForm_ShouldAllowAnyOrigin()
        {
            // Arrange
            const string origin = "https://anydomain.com";
            const string formId = "wildcard-form";
            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["*"], true);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            client.DefaultRequestHeaders.Add("Origin", origin);

            // Act - Test form-specific endpoint
            var response = await client.GetAsync($"/v1/form/{formId}");

            // Assert
            response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin" && h.Value.Contains(origin));
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithDisallowedOrigin_ShouldNotSetCorsHeaders()
        {
            // Arrange
            const string origin = "https://blocked.com";
            client.DefaultRequestHeaders.Add("Origin", origin);

            // Act - Test generic endpoint with origin not in AllowedOrigins configuration
            var response = await client.GetAsync("/");

            // Assert
            response.Headers.Should().NotContain(h => h.Key == "Access-Control-Allow-Origin");
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithNoOriginHeader_ShouldNotSetCorsHeaders()
        {
            // Arrange - No origin header is added

            // Act
            var response = await client.GetAsync("/");

            // Assert
            response.Headers.Should().NotContain(h => h.Key == "Access-Control-Allow-Origin");
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithEmptyOrigin_ShouldNotSetCorsHeaders()
        {
            // Arrange
            client.DefaultRequestHeaders.Add("Origin", string.Empty);

            // Act
            var response = await client.GetAsync("/");

            // Assert
            response.Headers.Should().NotContain(h => h.Key == "Access-Control-Allow-Origin");
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithOptionsRequest_ShouldReturn204()
        {
            // Arrange
            const string origin = "https://example.com";
            client.DefaultRequestHeaders.Add("Origin", origin);

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Options, "/"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin" && h.Value.Contains(origin));
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithOptionsRequestNoOrigin_ShouldReturn204()
        {
            // Arrange - No origin header is added

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Options, "/"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithDisabledWildcardForm_ShouldNotAllowAnyOrigin()
        {
            // Arrange
            const string origin = "https://anydomain.com";
            const string formId = "disabled-form";
            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["*"], false); // Disabled form

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            client.DefaultRequestHeaders.Add("Origin", origin);

            // Act - Test form-specific endpoint with disabled form
            var response = await client.GetAsync($"/v1/form/{formId}");

            // Assert
            response.Headers.Should().NotContain(h => h.Key == "Access-Control-Allow-Origin");
        }

        [Theory]
        [InlineData("https://example.com")]
        [InlineData("http://localhost:3000")]
        [InlineData("https://subdomain.example.com")]
        public async Task DynamicCorsMiddleware_WithMultipleOrigins_ShouldHandleCorrectly(string testOrigin)
        {
            // Arrange - Origins are configured in the test configuration
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Origin", testOrigin);

            // Act - Test generic endpoint (uses AllowedOrigins from configuration)
            var response = await client.GetAsync("/");

            // Assert
            response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin" && h.Value.Contains(testOrigin));
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithFormSpecificOrigin_ShouldUseFormConfiguration()
        {
            // Arrange
            const string origin = "https://form-specific.com";
            const string formId = "test-form";
            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, [origin], true);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            client.DefaultRequestHeaders.Add("Origin", origin);

            // Act - Test form-specific endpoint
            var response = await client.PostAsync($"/v1/form/{formId}", new StringContent(string.Empty, Encoding.UTF8, "application/x-www-form-urlencoded"));

            // Assert
            response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin" && h.Value.Contains(origin));
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithFormSpecificDisallowedOrigin_ShouldNotSetCorsHeaders()
        {
            // Arrange
            const string origin = "https://blocked.com";
            const string formId = "restricted-form";
            var formConfig = TestDataBuilder.CreateFormConfiguration(formId, ["https://allowed-only.com"], true);

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync(formConfig);

            client.DefaultRequestHeaders.Add("Origin", origin);

            // Act - Test form-specific endpoint
            var response = await client.PostAsync($"/v1/form/{formId}", new StringContent(string.Empty, Encoding.UTF8, "application/x-www-form-urlencoded"));

            // Assert
            response.Headers.Should().NotContain(h => h.Key == "Access-Control-Allow-Origin");
        }

        [Fact]
        public async Task DynamicCorsMiddleware_WithNonExistentForm_ShouldNotSetCorsHeaders()
        {
            // Arrange
            const string origin = "https://example.com";
            const string formId = "non-existent-form";

            formConfigServiceMock.Setup(x => x.GetFormConfigurationAsync(formId))
                                  .ReturnsAsync((FormConfiguration?)null);

            client.DefaultRequestHeaders.Add("Origin", origin);

            // Act - Test form-specific endpoint
            var response = await client.PostAsync($"/v1/form/{formId}", new StringContent(string.Empty, Encoding.UTF8, "application/x-www-form-urlencoded"));

            // Assert
            response.Headers.Should().NotContain(h => h.Key == "Access-Control-Allow-Origin");
        }

        public async ValueTask DisposeAsync()
        {
            client?.Dispose();
            if (host != null)
            {
                await host.StopAsync();
                host.Dispose();
            }
        }
    }

    public class ClientInfoMiddlewareTests : IAsyncDisposable
    {
        private readonly IHost host;
        private readonly HttpClient client;
        private readonly Mock<ILogger<ClientInfoMiddleware>> loggerMock;

        public ClientInfoMiddlewareTests()
        {
            loggerMock = new Mock<ILogger<ClientInfoMiddleware>>();

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddSingleton(loggerMock.Object);
                    });
                    webHost.Configure(app =>
                    {
                        app.UseMiddleware<ClientInfoMiddleware>();
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("OK");
                        });
                    });
                });

            host = hostBuilder.Build();
            host.Start();
            client = host.GetTestClient();
        }

        [Fact]
        public async Task ClientInfoMiddleware_ShouldPassRequestToNextMiddleware()
        {
            // Act
            var response = await client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            content.Should().Be("OK");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        public async ValueTask DisposeAsync()
        {
            client?.Dispose();
            if (host != null)
            {
                await host.StopAsync();
                host.Dispose();
            }
        }
    }
}
