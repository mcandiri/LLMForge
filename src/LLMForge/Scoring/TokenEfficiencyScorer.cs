using LLMForge.Providers;

namespace LLMForge.Scoring;

/// <summary>
/// Scores responses based on token efficiency — fewer tokens for equivalent quality is better.
/// </summary>
public class TokenEfficiencyScorer : IResponseScorer
{
    /// <inheritdoc />
    public string Name => "TokenEfficiency";

    /// <inheritdoc />
    public Task<double> ScoreAsync(
        LLMResponse response,
        IReadOnlyList<LLMResponse> allResponses,
        CancellationToken cancellationToken = default)
    {
        var successfulResponses = allResponses.Where(r => r.IsSuccess && r.CompletionTokens > 0).ToList();

        if (successfulResponses.Count <= 1)
            return Task.FromResult(1.0);

        var maxTokens = successfulResponses.Max(r => r.CompletionTokens);
        var minTokens = successfulResponses.Min(r => r.CompletionTokens);

        if (maxTokens <= minTokens)
            return Task.FromResult(1.0);

        // Invert: fewer tokens = higher score
        var score = 1.0 - (double)(response.CompletionTokens - minTokens) / (maxTokens - minTokens);
        return Task.FromResult(Math.Max(0.0, Math.Min(1.0, score)));
    }
}
