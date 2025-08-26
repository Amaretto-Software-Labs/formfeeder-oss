namespace FormFeeder.Api.Services;

/// <summary>
/// Configuration options for retry policies.
/// </summary>
public sealed record RetryPolicyConfiguration
{
    /// <summary>
    /// Gets maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Gets base delay in seconds before first retry.
    /// </summary>
    public int BaseDelaySeconds { get; init; } = 1;

    /// <summary>
    /// Gets maximum delay in seconds between retries.
    /// </summary>
    public int MaxDelaySeconds { get; init; } = 30;

    /// <summary>
    /// Gets a value indicating whether whether to add jitter to retry delays.
    /// </summary>
    public bool UseJitter { get; init; } = true;

    /// <summary>
    /// Gets type of jitter to apply (Decorrelated, Full, etc.)
    /// </summary>
    public string JitterType { get; init; } = "Decorrelated";

    /// <summary>
    /// Gets type of backoff strategy (Exponential, Linear, Constant).
    /// </summary>
    public string BackoffType { get; init; } = "Exponential";
}
