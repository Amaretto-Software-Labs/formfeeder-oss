using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;

using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace FormFeeder.Api.Services;

/// <summary>
/// Factory implementation for creating retry policies with proper exception handling and logging.
/// </summary>
public sealed class RetryPolicyFactory : IRetryPolicyFactory
{
    private readonly RetryPolicyConfiguration configuration;
    private readonly ILogger<RetryPolicyFactory> logger;

    public RetryPolicyFactory(IOptions<RetryPolicyConfiguration> configuration, ILogger<RetryPolicyFactory> logger)
    {
        this.configuration = configuration.Value;
        this.logger = logger;
    }

    public ResiliencePipeline CreateHttpRetryPolicy()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(static result => result is HttpResponseMessage response && ShouldRetryHttpResponse(response)),
                MaxRetryAttempts = configuration.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(configuration.BaseDelaySeconds),
                MaxDelay = TimeSpan.FromSeconds(configuration.MaxDelaySeconds),
                BackoffType = ParseBackoffType(configuration.BackoffType),
                UseJitter = configuration.UseJitter,
                OnRetry = OnRetryAsync,
            })
            .Build();
    }

    public ResiliencePipeline CreateMailJetRetryPolicy()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<AuthenticationException>()
                    .Handle<SocketException>()
                    .Handle<InvalidOperationException>(ex => ex.Message.Contains("SSL") || ex.Message.Contains("TLS"))
                    .HandleResult(static result => result is HttpResponseMessage response && ShouldRetryHttpResponse(response)),
                MaxRetryAttempts = configuration.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(configuration.BaseDelaySeconds),
                MaxDelay = TimeSpan.FromSeconds(configuration.MaxDelaySeconds),
                BackoffType = ParseBackoffType(configuration.BackoffType),
                UseJitter = configuration.UseJitter,
                OnRetry = OnMailJetRetryAsync,
            })
            .Build();
    }

    public ResiliencePipeline CreateSlackRetryPolicy()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
                    .HandleResult(static result => result is HttpResponseMessage response && ShouldRetrySlackResponse(response)),
                MaxRetryAttempts = configuration.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(configuration.BaseDelaySeconds),
                MaxDelay = TimeSpan.FromSeconds(configuration.MaxDelaySeconds),
                BackoffType = ParseBackoffType(configuration.BackoffType),
                UseJitter = configuration.UseJitter,
                OnRetry = OnSlackRetryAsync,
            })
            .Build();
    }

    private static bool ShouldRetryHttpResponse(HttpResponseMessage response)
    {
        return response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.GatewayTimeout => true,
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.InternalServerError => true,
            _ => false,
        };
    }

    private static bool ShouldRetrySlackResponse(HttpResponseMessage response)
    {
        // Slack-specific retry logic - they return specific status codes
        return response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.GatewayTimeout => true,
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.InternalServerError => true,
            _ => false,
        };
    }

    private DelayBackoffType ParseBackoffType(string backoffType) => backoffType.ToUpperInvariant() switch
    {
        "EXPONENTIAL" => DelayBackoffType.Exponential,
        "LINEAR" => DelayBackoffType.Linear,
        "CONSTANT" => DelayBackoffType.Constant,
        _ => DelayBackoffType.Exponential,
    };

    private async ValueTask OnRetryAsync(OnRetryArguments<object> args)
    {
        var exception = args.Outcome.Exception;
        var result = args.Outcome.Result;

        if (exception != null)
        {
            logger.LogWarning(
                "HTTP retry attempt {AttemptNumber} of {MaxAttempts} due to exception: {ExceptionType} - {Message}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                configuration.MaxRetryAttempts,
                exception.GetType().Name,
                exception.Message,
                args.RetryDelay.TotalMilliseconds);
        }
        else if (result is HttpResponseMessage httpResponse)
        {
            logger.LogWarning(
                "HTTP retry attempt {AttemptNumber} of {MaxAttempts} due to status code: {StatusCode}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                configuration.MaxRetryAttempts,
                httpResponse.StatusCode,
                args.RetryDelay.TotalMilliseconds);
        }

        await Task.CompletedTask;
    }

    private async ValueTask OnMailJetRetryAsync(OnRetryArguments<object> args)
    {
        var exception = args.Outcome.Exception;
        var result = args.Outcome.Result;

        if (exception != null)
        {
            logger.LogWarning(
                "MailJet retry attempt {AttemptNumber} of {MaxAttempts} due to exception: {ExceptionType} - {Message}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                configuration.MaxRetryAttempts,
                exception.GetType().Name,
                exception.Message,
                args.RetryDelay.TotalMilliseconds);
        }
        else if (result is HttpResponseMessage httpResponse)
        {
            logger.LogWarning(
                "MailJet retry attempt {AttemptNumber} of {MaxAttempts} due to status code: {StatusCode}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                configuration.MaxRetryAttempts,
                httpResponse.StatusCode,
                args.RetryDelay.TotalMilliseconds);
        }

        await Task.CompletedTask;
    }

    private async ValueTask OnSlackRetryAsync(OnRetryArguments<object> args)
    {
        var exception = args.Outcome.Exception;
        var result = args.Outcome.Result;

        if (exception != null)
        {
            logger.LogWarning(
                "Slack retry attempt {AttemptNumber} of {MaxAttempts} due to exception: {ExceptionType} - {Message}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                configuration.MaxRetryAttempts,
                exception.GetType().Name,
                exception.Message,
                args.RetryDelay.TotalMilliseconds);
        }
        else if (result is HttpResponseMessage httpResponse)
        {
            logger.LogWarning(
                "Slack retry attempt {AttemptNumber} of {MaxAttempts} due to status code: {StatusCode}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                configuration.MaxRetryAttempts,
                httpResponse.StatusCode,
                args.RetryDelay.TotalMilliseconds);
        }

        await Task.CompletedTask;
    }
}
