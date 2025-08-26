using FormFeeder.Api.Models;

namespace FormFeeder.Api.Tests.Models;

public class ResultTests
{
    public class Success
    {
        [Fact]
        public void CreateSuccess_WithValue_ShouldReturnSuccessfulResult()
        {
            // Arrange
            const string testValue = "test";

            // Act
            var result = Result.Success(testValue);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(testValue);
            result.Error.Should().BeNull();
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void CreateSuccess_WithNullValue_ShouldReturnSuccessfulResult()
        {
            // Arrange
            string? testValue = null;

            // Act
            var result = Result.Success(testValue);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeNull();
            result.Error.Should().BeNull();
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void ImplicitConversion_FromValue_ShouldCreateSuccessResult()
        {
            // Arrange
            const int testValue = 42;

            // Act
            Result<int> result = testValue;

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(testValue);
            result.Error.Should().BeNull();
            result.Exception.Should().BeNull();
        }
    }

    public class Failure
    {
        [Fact]
        public void CreateFailure_WithError_ShouldReturnFailedResult()
        {
            // Arrange
            const string errorMessage = "Something went wrong";

            // Act
            var result = Result.Failure<string>(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.Error.Should().Be(errorMessage);
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void CreateFailure_WithErrorAndException_ShouldReturnFailedResult()
        {
            // Arrange
            const string errorMessage = "Something went wrong";
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = Result.Failure<string>(errorMessage, exception);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.Error.Should().Be(errorMessage);
            result.Exception.Should().Be(exception);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateFailure_WithEmptyOrWhitespaceError_ShouldAcceptError(string errorMessage)
        {
            // Act
            var result = Result.Failure<string>(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(errorMessage);
        }
    }

    public class Match
    {
        [Fact]
        public void Match_WhenSuccess_ShouldExecuteOnSuccess()
        {
            // Arrange
            const string value = "test";
            var result = Result.Success(value);

            // Act
            var output = result.Match(
                onSuccess: v => $"Success: {v}",
                onFailure: e => $"Failure: {e}");

            // Assert
            output.Should().Be("Success: test");
        }

        [Fact]
        public void Match_WhenFailure_ShouldExecuteOnFailure()
        {
            // Arrange
            const string error = "error message";
            var result = Result.Failure<string>(error);

            // Act
            var output = result.Match(
                onSuccess: v => $"Success: {v}",
                onFailure: e => $"Failure: {e}");

            // Assert
            output.Should().Be("Failure: error message");
        }

        [Fact]
        public void Match_WhenSuccessWithNullValue_ShouldExecuteOnFailure()
        {
            // Arrange
            var result = new Result<string>
            {
                IsSuccess = true,
                Value = null,
                Error = null,
            };

            // Act
            var output = result.Match(
                onSuccess: v => $"Success: {v}",
                onFailure: e => $"Failure: {e}");

            // Assert
            output.Should().Be("Failure: Unknown error");
        }

        [Fact]
        public async Task MatchAsync_WhenSuccess_ShouldExecuteOnSuccess()
        {
            // Arrange
            const string value = "test";
            var result = Result.Success(value);

            // Act
            var output = await result.MatchAsync(
                onSuccess: async v =>
                {
                    await Task.Delay(1);
                    return $"Success: {v}";
                },
                onFailure: async e =>
                {
                    await Task.Delay(1);
                    return $"Failure: {e}";
                });

            // Assert
            output.Should().Be("Success: test");
        }

        [Fact]
        public async Task MatchAsync_WhenFailure_ShouldExecuteOnFailure()
        {
            // Arrange
            const string error = "error message";
            var result = Result.Failure<string>(error);

            // Act
            var output = await result.MatchAsync(
                onSuccess: async v =>
                {
                    await Task.Delay(1);
                    return $"Success: {v}";
                },
                onFailure: async e =>
                {
                    await Task.Delay(1);
                    return $"Failure: {e}";
                });

            // Assert
            output.Should().Be("Failure: error message");
        }
    }

    public class StaticMethods
    {
        [Fact]
        public void StaticSuccess_ShouldCreateSuccessResult()
        {
            // Arrange
            const int value = 42;

            // Act
            var result = Result.Success(value);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(value);
        }

        [Fact]
        public void StaticFailure_WithError_ShouldCreateFailureResult()
        {
            // Arrange
            const string error = "test error";

            // Act
            var result = Result.Failure<int>(error);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(error);
        }

        [Fact]
        public void StaticFailure_WithErrorAndException_ShouldCreateFailureResult()
        {
            // Arrange
            const string error = "test error";
            var exception = new Exception("test");

            // Act
            var result = Result.Failure<int>(error, exception);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(error);
            result.Exception.Should().Be(exception);
        }
    }

    public class EdgeCases
    {
        [Fact]
        public void Result_WithNullError_ShouldHandleGracefully()
        {
            // Arrange & Act
            var result = Result.Failure<string>(null!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeNull();
        }

        [Fact]
        public void Match_WithNullError_ShouldUseUnknownError()
        {
            // Arrange
            var result = new Result<string>
            {
                IsSuccess = false,
                Value = null,
                Error = null,
            };

            // Act
            var output = result.Match(
                onSuccess: v => $"Success: {v}",
                onFailure: e => $"Failure: {e}");

            // Assert
            output.Should().Be("Failure: Unknown error");
        }
    }
}
