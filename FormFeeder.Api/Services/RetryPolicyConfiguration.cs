namespace FormFeeder.Api.Services;

/// <summary>
/// Configuration options for retry policies
/// </summary>
public sealed record RetryPolicyConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Base delay in seconds before first retry
    /// </summary>
    public int BaseDelaySeconds { get; init; } = 1;

    /// <summary>
    /// Maximum delay in seconds between retries
    /// </summary>
    public int MaxDelaySeconds { get; init; } = 30;

    /// <summary>
    /// Whether to add jitter to retry delays
    /// </summary>
    public bool UseJitter { get; init; } = true;

    /// <summary>
    /// Type of jitter to apply (Decorrelated, Full, etc.)
    /// </summary>
    public string JitterType { get; init; } = "Decorrelated";

    /// <summary>
    /// Type of backoff strategy (Exponential, Linear, Constant)
    /// </summary>
    public string BackoffType { get; init; } = "Exponential";
}