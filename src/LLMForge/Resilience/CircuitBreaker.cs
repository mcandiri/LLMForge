using LLMForge.Configuration;

namespace LLMForge.Resilience;

/// <summary>
/// Implements the circuit breaker pattern for provider resilience.
/// States: Closed (normal) → Open (blocked) → HalfOpen (probing) → Closed.
/// </summary>
public class CircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly object _lock = new();

    private CircuitState _state = CircuitState.Closed;
    private int _consecutiveFailures;
    private int _halfOpenSuccesses;
    private DateTimeOffset _openedAt;

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open && ShouldTransitionToHalfOpen())
                {
                    _state = CircuitState.HalfOpen;
                    _halfOpenSuccesses = 0;
                }
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets the number of consecutive failures recorded.
    /// </summary>
    public int ConsecutiveFailures
    {
        get { lock (_lock) { return _consecutiveFailures; } }
    }

    /// <summary>
    /// Initializes a new circuit breaker with the given options.
    /// </summary>
    public CircuitBreaker(CircuitBreakerOptions? options = null)
    {
        _options = options ?? new CircuitBreakerOptions();
    }

    /// <summary>
    /// Returns true if the circuit allows a request to proceed.
    /// </summary>
    public bool AllowRequest()
    {
        lock (_lock)
        {
            if (!_options.Enabled) return true;

            switch (_state)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    if (ShouldTransitionToHalfOpen())
                    {
                        _state = CircuitState.HalfOpen;
                        _halfOpenSuccesses = 0;
                        return true; // Allow probe request
                    }
                    return false;

                case CircuitState.HalfOpen:
                    return true; // Allow probe requests

                default:
                    return true;
            }
        }
    }

    /// <summary>
    /// Records a successful request.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitState.HalfOpen:
                    _halfOpenSuccesses++;
                    if (_halfOpenSuccesses >= _options.SuccessThresholdInHalfOpen)
                    {
                        _state = CircuitState.Closed;
                        _consecutiveFailures = 0;
                    }
                    break;

                case CircuitState.Closed:
                    _consecutiveFailures = 0;
                    break;
            }
        }
    }

    /// <summary>
    /// Records a failed request.
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            _consecutiveFailures++;

            switch (_state)
            {
                case CircuitState.HalfOpen:
                    // Any failure in half-open goes back to open
                    _state = CircuitState.Open;
                    _openedAt = DateTimeOffset.UtcNow;
                    break;

                case CircuitState.Closed:
                    if (_consecutiveFailures >= _options.FailureThreshold)
                    {
                        _state = CircuitState.Open;
                        _openedAt = DateTimeOffset.UtcNow;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Manually resets the circuit breaker to the closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitState.Closed;
            _consecutiveFailures = 0;
            _halfOpenSuccesses = 0;
        }
    }

    private bool ShouldTransitionToHalfOpen()
    {
        return DateTimeOffset.UtcNow - _openedAt >= _options.OpenDuration;
    }
}

/// <summary>
/// Represents the state of a circuit breaker.
/// </summary>
public enum CircuitState
{
    /// <summary>Circuit is closed — requests flow normally.</summary>
    Closed,

    /// <summary>Circuit is open — requests are blocked.</summary>
    Open,

    /// <summary>Circuit is half-open — limited probe requests are allowed.</summary>
    HalfOpen
}
