using Polly;

namespace FormFeeder.Api.Services;

/// <summary>
/// Factory for creating standardized retry policies for external service calls
/// </summary>
public interface IRetryPolicyFactory
{
    /// <summary>
    /// Creates a retry policy for HTTP operations with exponential backoff and jitter
    /// </summary>
    /// <returns>A retry policy suitable for HTTP operations</returns>
    ResiliencePipeline CreateHttpRetryPolicy();

    /// <summary>
    /// Creates a retry policy specifically for MailJet API calls with SSL/TLS handling
    /// </summary>
    /// <returns>A retry policy optimized for MailJet operations</returns>
    ResiliencePipeline CreateMailJetRetryPolicy();

    /// <summary>
    /// Creates a retry policy for Slack webhook calls
    /// </summary>
    /// <returns>A retry policy optimized for Slack webhook operations</returns>
    ResiliencePipeline CreateSlackRetryPolicy();
}