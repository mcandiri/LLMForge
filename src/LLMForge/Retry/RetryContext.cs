using System.Net;
using LLMForge.Resilience;

namespace LLMForge.Retry;

/// <summary>
/// Provides context information for retry attempts.
/// </summary>
public class RetryContext
{
    /// <summary>
    /// Gets or sets the current attempt number (1-based).
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of attempts allowed.
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// Gets or sets the exception from the last failed attempt, if any.
    /// </summary>
    public Exception? LastException { get; set; }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets whether this is the last allowed attempt.
    /// </summary>
    public bool IsLastAttempt => AttemptNumber >= MaxAttempts;

    /// <summary>
    /// Gets whether more retry attempts are available.
    /// </summary>
    public bool CanRetry => AttemptNumber < MaxAttempts;

    /// <summary>
    /// Gets or sets rate-limit information from the last failed response, if applicable.
    /// </summary>
    public RateLimitInfo? RateLimitInfo { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code from the last failed response, if applicable.
    /// </summary>
    public HttpStatusCode? HttpStatusCode { get; set; }
}
