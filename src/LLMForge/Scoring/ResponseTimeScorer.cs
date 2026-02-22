using LLMForge.Providers;

namespace LLMForge.Scoring;

/// <summary>
/// Scores responses based on response time — faster responses get higher scores.
/// </summary>
public class ResponseTimeScorer : IResponseScorer
{
    /// <inheritdoc />
    public string Name => "ResponseTime";

    /// <inheritdoc />
    public Task<double> ScoreAsync(
        LLMResponse response,
        IReadOnlyList<LLMResponse> allResponses,
        CancellationToken cancellationToken = default)
    {
        var successfulResponses = allResponses.Where(r => r.IsSuccess).ToList();

        if (successfulResponses.Count <= 1)
            return Task.FromResult(1.0);

        var maxDuration = successfulResponses.Max(r => r.Duration.TotalMilliseconds);
        var minDuration = successfulResponses.Min(r => r.Duration.TotalMilliseconds);

        if (maxDuration <= minDuration)
            return Task.FromResult(1.0);

        // Invert: fastest gets 1.0, slowest gets 0.0
        var score = 1.0 - (response.Duration.TotalMilliseconds - minDuration) / (maxDuration - minDuration);
        return Task.FromResult(Math.Max(0.0, Math.Min(1.0, score)));
    }
}
