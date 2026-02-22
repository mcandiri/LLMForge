namespace LLMForge.Retry;

/// <summary>
/// Implements a fixed delay between retry attempts.
/// </summary>
public class FixedDelayPolicy : IRetryPolicy
{
    private readonly TimeSpan _delay;

    /// <inheritdoc />
    public string Name => "FixedDelay";

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDelayPolicy"/>.
    /// </summary>
    /// <param name="delay">The fixed delay between retries. Defaults to 2 seconds.</param>
    public FixedDelayPolicy(TimeSpan? delay = null)
    {
        _delay = delay ?? TimeSpan.FromSeconds(2);
    }

    /// <inheritdoc />
    public Task<TimeSpan?> GetDelayAsync(RetryContext context, CancellationToken cancellationToken = default)
    {
        return context.CanRetry
            ? Task.FromResult<TimeSpan?>(_delay)
            : Task.FromResult<TimeSpan?>(null);
    }
}
