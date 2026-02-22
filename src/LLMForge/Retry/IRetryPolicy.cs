namespace LLMForge.Retry;

/// <summary>
/// Defines a retry policy that determines delay and whether to retry.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Gets the name of this retry policy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether a retry should be attempted and the delay before the next attempt.
    /// </summary>
    /// <param name="context">The current retry context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delay before the next attempt, or null if no retry should be attempted.</returns>
    Task<TimeSpan?> GetDelayAsync(RetryContext context, CancellationToken cancellationToken = default);
}
