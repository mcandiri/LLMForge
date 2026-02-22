namespace LLMForge.Retry;

/// <summary>
/// Implements exponential backoff with optional jitter for retry delays.
/// </summary>
public class ExponentialBackoffPolicy : IRetryPolicy
{
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;
    private readonly bool _useJitter;

    /// <inheritdoc />
    public string Name => "ExponentialBackoff";

    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialBackoffPolicy"/>.
    /// </summary>
    /// <param name="baseDelay">The base delay for the first retry. Defaults to 1 second.</param>
    /// <param name="maxDelay">The maximum delay cap. Defaults to 30 seconds.</param>
    /// <param name="useJitter">Whether to add random jitter to prevent thundering herd.</param>
    public ExponentialBackoffPolicy(
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null,
        bool useJitter = true)
    {
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        _maxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        _useJitter = useJitter;
    }

    /// <inheritdoc />
    public Task<TimeSpan?> GetDelayAsync(RetryContext context, CancellationToken cancellationToken = default)
    {
        if (!context.CanRetry)
            return Task.FromResult<TimeSpan?>(null);

        var exponentialDelay = TimeSpan.FromMilliseconds(
            _baseDelay.TotalMilliseconds * Math.Pow(2, context.AttemptNumber - 1));

        if (exponentialDelay > _maxDelay)
            exponentialDelay = _maxDelay;

        if (_useJitter)
        {
            var jitter = Random.Shared.NextDouble() * 0.3 * exponentialDelay.TotalMilliseconds;
            exponentialDelay = exponentialDelay.Add(TimeSpan.FromMilliseconds(jitter));
        }

        return Task.FromResult<TimeSpan?>(exponentialDelay);
    }
}
