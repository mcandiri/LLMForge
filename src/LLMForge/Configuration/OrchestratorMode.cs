namespace LLMForge.Configuration;

/// <summary>
/// Defines how multiple LLM providers are executed during orchestration.
/// </summary>
public enum ExecutionStrategy
{
    /// <summary>
    /// Execute all configured providers simultaneously and compare results.
    /// </summary>
    Parallel,

    /// <summary>
    /// Execute providers one at a time, stopping on the first successful and valid response.
    /// </summary>
    Sequential,

    /// <summary>
    /// Try providers in a specified order, falling back to the next on failure.
    /// </summary>
    Fallback
}

/// <summary>
/// Defines how the best response is selected from multiple provider responses.
/// </summary>
public enum ConsensusStrategy
{
    /// <summary>
    /// Select the response with the highest composite score.
    /// </summary>
    HighestScore,

    /// <summary>
    /// Select the response that most models agree on.
    /// </summary>
    MajorityVote,

    /// <summary>
    /// Require a minimum number of models to agree before accepting a response.
    /// </summary>
    Quorum
}

/// <summary>
/// Defines conditions that trigger a fallback to the next provider.
/// </summary>
[Flags]
public enum FallbackTrigger
{
    /// <summary>No fallback triggers.</summary>
    None = 0,

    /// <summary>Fallback when the provider times out.</summary>
    Timeout = 1,

    /// <summary>Fallback when response validation fails.</summary>
    ValidationFailure = 2,

    /// <summary>Fallback on any exception.</summary>
    Exception = 4,

    /// <summary>Fallback on all error conditions.</summary>
    All = Timeout | ValidationFailure | Exception
}

/// <summary>
/// Defines the retry policy type.
/// </summary>
public enum RetryPolicy
{
    /// <summary>No retry.</summary>
    None,

    /// <summary>Fixed delay between retries.</summary>
    FixedDelay,

    /// <summary>Exponentially increasing delay between retries.</summary>
    ExponentialBackoff
}
