using FluentAssertions;
using LLMForge.Retry;
using Xunit;

namespace LLMForge.Tests.Retry;

public class ExponentialBackoffTests
{
    [Fact]
    public async Task GetDelayAsync_FirstAttempt_ShouldReturnBaseDelay()
    {
        // Arrange
        var sut = new ExponentialBackoffPolicy(
            baseDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(30),
            useJitter: false);

        var context = new RetryContext
        {
            AttemptNumber = 1,
            MaxAttempts = 5
        };

        // Act
        var delay = await sut.GetDelayAsync(context);

        // Assert
        delay.Should().NotBeNull();
        delay!.Value.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetDelayAsync_SubsequentAttempts_ShouldIncreaseExponentially()
    {
        // Arrange
        var sut = new ExponentialBackoffPolicy(
            baseDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(60),
            useJitter: false);

        // Act
        var delay1 = await sut.GetDelayAsync(new RetryContext { AttemptNumber = 1, MaxAttempts = 5 });
        var delay2 = await sut.GetDelayAsync(new RetryContext { AttemptNumber = 2, MaxAttempts = 5 });
        var delay3 = await sut.GetDelayAsync(new RetryContext { AttemptNumber = 3, MaxAttempts = 5 });

        // Assert
        delay1!.Value.TotalSeconds.Should().Be(1);   // 1 * 2^0 = 1
        delay2!.Value.TotalSeconds.Should().Be(2);   // 1 * 2^1 = 2
        delay3!.Value.TotalSeconds.Should().Be(4);   // 1 * 2^2 = 4
    }

    [Fact]
    public async Task GetDelayAsync_ExceedsMaxDelay_ShouldCapAtMaxDelay()
    {
        // Arrange
        var sut = new ExponentialBackoffPolicy(
            baseDelay: TimeSpan.FromSeconds(10),
            maxDelay: TimeSpan.FromSeconds(30),
            useJitter: false);

        var context = new RetryContext
        {
            AttemptNumber = 5,
            MaxAttempts = 10
        };

        // Act
        var delay = await sut.GetDelayAsync(context);

        // Assert
        delay.Should().NotBeNull();
        delay!.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task GetDelayAsync_LastAttemptReached_ShouldReturnNull()
    {
        // Arrange
        var sut = new ExponentialBackoffPolicy(useJitter: false);
        var context = new RetryContext
        {
            AttemptNumber = 3,
            MaxAttempts = 3
        };

        // Act
        var delay = await sut.GetDelayAsync(context);

        // Assert
        delay.Should().BeNull();
    }

    [Fact]
    public async Task GetDelayAsync_WithJitter_ShouldReturnSlightlyMoreThanBase()
    {
        // Arrange
        var sut = new ExponentialBackoffPolicy(
            baseDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(30),
            useJitter: true);

        var context = new RetryContext
        {
            AttemptNumber = 1,
            MaxAttempts = 5
        };

        // Act
        var delay = await sut.GetDelayAsync(context);

        // Assert
        delay.Should().NotBeNull();
        delay!.Value.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(1));
        // Jitter adds up to 30% of base
        delay.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(1.3));
    }
}
