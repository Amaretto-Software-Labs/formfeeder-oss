using FormFeeder.Api.Services;
using FormFeeder.Api.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FormFeeder.Api.Tests.Services;

public class RetryPolicyFactoryTests : TestBase
{
    private readonly RetryPolicyFactory _factory;
    private readonly Mock<ILogger<RetryPolicyFactory>> _loggerMock;

    public RetryPolicyFactoryTests()
    {
        _loggerMock = CreateMock<ILogger<RetryPolicyFactory>>();
        var configuration = new RetryPolicyConfiguration
        {
            MaxRetryAttempts = 3,
            BaseDelaySeconds = 1,
            MaxDelaySeconds = 30,
            UseJitter = true,
            JitterType = "Decorrelated",
            BackoffType = "Exponential"
        };
        var optionsMock = CreateMock<IOptions<RetryPolicyConfiguration>>();
        optionsMock.Setup(x => x.Value).Returns(configuration);
        
        _factory = new RetryPolicyFactory(optionsMock.Object, _loggerMock.Object);
    }

    public class CreateHttpRetryPolicy : RetryPolicyFactoryTests
    {
        [Fact]
        public void CreateHttpRetryPolicy_ShouldReturnValidPipeline()
        {
            // Act
            var policy = _factory.CreateHttpRetryPolicy();

            // Assert
            policy.Should().NotBeNull();
        }
    }

    public class CreateMailJetRetryPolicy : RetryPolicyFactoryTests
    {
        [Fact]
        public void CreateMailJetRetryPolicy_ShouldReturnValidPipeline()
        {
            // Act
            var policy = _factory.CreateMailJetRetryPolicy();

            // Assert
            policy.Should().NotBeNull();
        }
    }

    public class CreateSlackRetryPolicy : RetryPolicyFactoryTests
    {
        [Fact]
        public void CreateSlackRetryPolicy_ShouldReturnValidPipeline()
        {
            // Act
            var policy = _factory.CreateSlackRetryPolicy();

            // Assert
            policy.Should().NotBeNull();
        }
    }

    public class Configuration : RetryPolicyFactoryTests
    {
        [Theory]
        [InlineData("Exponential")]
        [InlineData("Linear")]
        [InlineData("Constant")]
        [InlineData("Invalid")]
        public void CreateHttpRetryPolicy_WithDifferentBackoffTypes_ShouldNotThrow(string backoffType)
        {
            // Arrange
            var configuration = new RetryPolicyConfiguration
            {
                MaxRetryAttempts = 2,
                BaseDelaySeconds = 1,
                MaxDelaySeconds = 10,
                BackoffType = backoffType
            };
            var optionsMock = CreateMock<IOptions<RetryPolicyConfiguration>>();
            optionsMock.Setup(x => x.Value).Returns(configuration);
            var factory = new RetryPolicyFactory(optionsMock.Object, _loggerMock.Object);

            // Act & Assert
            var action = () => factory.CreateHttpRetryPolicy();
            action.Should().NotThrow();
        }

        [Fact]
        public void CreateHttpRetryPolicy_WithMinimumRetries_ShouldNotThrow()
        {
            // Arrange
            var configuration = new RetryPolicyConfiguration
            {
                MaxRetryAttempts = 1, // Minimum allowed by Polly
                BaseDelaySeconds = 1,
                MaxDelaySeconds = 10
            };
            var optionsMock = CreateMock<IOptions<RetryPolicyConfiguration>>();
            optionsMock.Setup(x => x.Value).Returns(configuration);
            var factory = new RetryPolicyFactory(optionsMock.Object, _loggerMock.Object);

            // Act & Assert
            var action = () => factory.CreateHttpRetryPolicy();
            action.Should().NotThrow();
        }
    }
}