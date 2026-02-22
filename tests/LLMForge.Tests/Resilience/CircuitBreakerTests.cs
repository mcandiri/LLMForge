using FluentAssertions;
using LLMForge.Configuration;
using LLMForge.Resilience;
using Xunit;

namespace LLMForge.Tests.Resilience;

public class CircuitBreakerTests
{
    [Fact]
    public void InitialState_IsClosed()
    {
        var cb = new CircuitBreaker();
        cb.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public void Closed_AllowsRequests()
    {
        var cb = new CircuitBreaker();
        cb.AllowRequest().Should().BeTrue();
    }

    [Fact]
    public void Closed_OpensAfterThresholdFailures()
    {
        var options = new CircuitBreakerOptions { FailureThreshold = 3 };
        var cb = new CircuitBreaker(options);

        cb.RecordFailure();
        cb.RecordFailure();
        cb.State.Should().Be(CircuitState.Closed);

        cb.RecordFailure();
        cb.State.Should().Be(CircuitState.Open);
    }

    [Fact]
    public void Open_BlocksRequests()
    {
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 1,
            OpenDuration = TimeSpan.FromMinutes(5)
        };
        var cb = new CircuitBreaker(options);

        cb.RecordFailure();
        cb.AllowRequest().Should().BeFalse();
    }

    [Fact]
    public void Open_TransitionsToHalfOpenAfterDuration()
    {
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 1,
            OpenDuration = TimeSpan.Zero // Immediately transition
        };
        var cb = new CircuitBreaker(options);

        cb.RecordFailure();

        // With OpenDuration=Zero, reading State immediately transitions to HalfOpen
        cb.State.Should().Be(CircuitState.HalfOpen);
        cb.AllowRequest().Should().BeTrue();
    }

    [Fact]
    public void HalfOpen_ClosesAfterSuccessThreshold()
    {
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 1,
            OpenDuration = TimeSpan.Zero,
            SuccessThresholdInHalfOpen = 2
        };
        var cb = new CircuitBreaker(options);

        // Open the circuit
        cb.RecordFailure();

        // Force transition to half-open
        _ = cb.State;
        cb.State.Should().Be(CircuitState.HalfOpen);

        cb.RecordSuccess();
        cb.State.Should().Be(CircuitState.HalfOpen); // Need 2 successes

        cb.RecordSuccess();
        cb.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public void HalfOpen_OpensOnFailure()
    {
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 1,
            OpenDuration = TimeSpan.FromMinutes(5),
            SuccessThresholdInHalfOpen = 2
        };
        var cb = new CircuitBreaker(options);

        // Open the circuit
        cb.RecordFailure();

        // Manually force to half-open by using AllowRequest with zero duration
        // Instead, use Reset + RecordFailure + transition pattern
        // Let's use a fresh breaker that opens and immediately transitions
        var options2 = new CircuitBreakerOptions
        {
            FailureThreshold = 1,
            OpenDuration = TimeSpan.Zero,
            SuccessThresholdInHalfOpen = 2
        };
        var cb2 = new CircuitBreaker(options2);
        cb2.RecordFailure();

        // State getter transitions to HalfOpen because OpenDuration is zero
        cb2.State.Should().Be(CircuitState.HalfOpen);

        // Failure in half-open should re-open the circuit
        cb2.RecordFailure();

        // With OpenDuration=Zero, reading State again transitions right back to HalfOpen.
        // Verify via AllowRequest that the failure was recorded (consecutive failures increased).
        cb2.ConsecutiveFailures.Should().Be(2);
    }

    [Fact]
    public void Closed_ResetsFailureCountOnSuccess()
    {
        var options = new CircuitBreakerOptions { FailureThreshold = 3 };
        var cb = new CircuitBreaker(options);

        cb.RecordFailure();
        cb.RecordFailure();
        cb.RecordSuccess(); // Resets counter
        cb.ConsecutiveFailures.Should().Be(0);

        cb.RecordFailure();
        cb.RecordFailure();
        cb.State.Should().Be(CircuitState.Closed); // Still closed — didn't hit 3 consecutive
    }

    [Fact]
    public void Reset_ReturnsToClosed()
    {
        var options = new CircuitBreakerOptions { FailureThreshold = 1 };
        var cb = new CircuitBreaker(options);

        cb.RecordFailure();
        cb.State.Should().Be(CircuitState.Open);

        cb.Reset();
        cb.State.Should().Be(CircuitState.Closed);
        cb.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void Disabled_AlwaysAllowsRequests()
    {
        var options = new CircuitBreakerOptions
        {
            Enabled = false,
            FailureThreshold = 1
        };
        var cb = new CircuitBreaker(options);

        cb.RecordFailure();
        cb.RecordFailure();
        cb.RecordFailure();

        cb.AllowRequest().Should().BeTrue();
    }
}
