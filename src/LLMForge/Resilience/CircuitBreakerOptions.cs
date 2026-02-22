namespace LLMForge.Configuration;

/// <summary>
/// Configuration options for the circuit breaker pattern.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the number of consecutive failures before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration the circuit stays open before transitioning to half-open.
    /// </summary>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the number of successful calls in half-open state required to close the circuit.
    /// </summary>
    public int SuccessThresholdInHalfOpen { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether the circuit breaker is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
