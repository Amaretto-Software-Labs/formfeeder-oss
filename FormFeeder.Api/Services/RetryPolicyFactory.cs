using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace FormFeeder.Api.Services;

/// <summary>
/// Factory implementation for creating retry policies with proper exception handling and logging
/// </summary>
public sealed class RetryPolicyFactory : IRetryPolicyFactory
{
    private readonly RetryPolicyConfiguration _configuration;
    private readonly ILogger<RetryPolicyFactory> _logger;

    public RetryPolicyFactory(IOptions<RetryPolicyConfiguration> configuration, ILogger<RetryPolicyFactory> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
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
                MaxRetryAttempts = _configuration.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(_configuration.BaseDelaySeconds),
                MaxDelay = TimeSpan.FromSeconds(_configuration.MaxDelaySeconds),
                BackoffType = ParseBackoffType(_configuration.BackoffType),
                UseJitter = _configuration.UseJitter,
                OnRetry = OnRetryAsync
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
                MaxRetryAttempts = _configuration.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(_configuration.BaseDelaySeconds),
                MaxDelay = TimeSpan.FromSeconds(_configuration.MaxDelaySeconds),
                BackoffType = ParseBackoffType(_configuration.BackoffType),
                UseJitter = _configuration.UseJitter,
                OnRetry = OnMailJetRetryAsync
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
                MaxRetryAttempts = _configuration.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(_configuration.BaseDelaySeconds),
                MaxDelay = TimeSpan.FromSeconds(_configuration.MaxDelaySeconds),
                BackoffType = ParseBackoffType(_configuration.BackoffType),
                UseJitter = _configuration.UseJitter,
                OnRetry = OnSlackRetryAsync
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
            _ => false
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
            _ => false
        };
    }

    private DelayBackoffType ParseBackoffType(string backoffType) => backoffType.ToUpperInvariant() switch
    {
        "EXPONENTIAL" => DelayBackoffType.Exponential,
        "LINEAR" => DelayBackoffType.Linear,
        "CONSTANT" => DelayBackoffType.Constant,
        _ => DelayBackoffType.Exponential
    };

    private async ValueTask OnRetryAsync(OnRetryArguments<object> args)
    {
        var exception = args.Outcome.Exception;
        var result = args.Outcome.Result;

        if (exception != null)
        {
            _logger.LogWarning("HTTP retry attempt {AttemptNumber} of {MaxAttempts} due to exception: {ExceptionType} - {Message}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                _configuration.MaxRetryAttempts,
                exception.GetType().Name,
                exception.Message,
                args.RetryDelay.TotalMilliseconds);
        }
        else if (result is HttpResponseMessage httpResponse)
        {
            _logger.LogWarning("HTTP retry attempt {AttemptNumber} of {MaxAttempts} due to status code: {StatusCode}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                _configuration.MaxRetryAttempts,
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
            _logger.LogWarning("MailJet retry attempt {AttemptNumber} of {MaxAttempts} due to exception: {ExceptionType} - {Message}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                _configuration.MaxRetryAttempts,
                exception.GetType().Name,
                exception.Message,
                args.RetryDelay.TotalMilliseconds);
        }
        else if (result is HttpResponseMessage httpResponse)
        {
            _logger.LogWarning("MailJet retry attempt {AttemptNumber} of {MaxAttempts} due to status code: {StatusCode}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                _configuration.MaxRetryAttempts,
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
            _logger.LogWarning("Slack retry attempt {AttemptNumber} of {MaxAttempts} due to exception: {ExceptionType} - {Message}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                _configuration.MaxRetryAttempts,
                exception.GetType().Name,
                exception.Message,
                args.RetryDelay.TotalMilliseconds);
        }
        else if (result is HttpResponseMessage httpResponse)
        {
            _logger.LogWarning("Slack retry attempt {AttemptNumber} of {MaxAttempts} due to status code: {StatusCode}. Delay: {Delay}ms",
                args.AttemptNumber + 1,
                _configuration.MaxRetryAttempts,
                httpResponse.StatusCode,
                args.RetryDelay.TotalMilliseconds);
        }

        await Task.CompletedTask;
    }
}