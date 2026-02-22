using LLMForge.Retry;

namespace LLMForge.Resilience;

/// <summary>
/// A retry policy that respects Retry-After headers from rate-limited responses.
/// Falls back to exponential backoff when no Retry-After header is present.
/// </summary>
public class RateLimitAwareRetryPolicy : IRetryPolicy
{
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;

    /// <inheritdoc />
    public string Name => "RateLimitAware";

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitAwareRetryPolicy"/>.
    /// </summary>
    /// <param name="baseDelay">Base delay for exponential backoff fallback.</param>
    /// <param name="maxDelay">Maximum delay cap.</param>
    public RateLimitAwareRetryPolicy(
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null)
    {
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        _maxDelay = maxDelay ?? TimeSpan.FromSeconds(60);
    }

    /// <inheritdoc />
    public Task<TimeSpan?> GetDelayAsync(RetryContext context, CancellationToken cancellationToken = default)
    {
        if (!context.CanRetry)
            return Task.FromResult<TimeSpan?>(null);

        // If we have rate limit info with Retry-After, use that
        if (context.RateLimitInfo?.RetryAfter is { } retryAfter)
        {
            var capped = retryAfter > _maxDelay ? _maxDelay : retryAfter;
            return Task.FromResult<TimeSpan?>(capped);
        }

        // Fall back to exponential backoff with jitter
        var exponentialDelay = TimeSpan.FromMilliseconds(
            _baseDelay.TotalMilliseconds * Math.Pow(2, context.AttemptNumber - 1));

        if (exponentialDelay > _maxDelay)
            exponentialDelay = _maxDelay;

        var jitter = Random.Shared.NextDouble() * 0.3 * exponentialDelay.TotalMilliseconds;
        exponentialDelay = exponentialDelay.Add(TimeSpan.FromMilliseconds(jitter));

        return Task.FromResult<TimeSpan?>(exponentialDelay);
    }
}
