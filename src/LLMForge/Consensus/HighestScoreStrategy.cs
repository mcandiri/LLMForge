using LLMForge.Scoring;

namespace LLMForge.Consensus;

/// <summary>
/// Selects the response with the highest composite score.
/// </summary>
public class HighestScoreStrategy : IConsensusStrategy
{
    /// <inheritdoc />
    public string Name => "HighestScore";

    /// <inheritdoc />
    public Task<ConsensusResult> EvaluateAsync(
        IReadOnlyList<ScoredResponse> scoredResponses,
        CancellationToken cancellationToken = default)
    {
        if (scoredResponses.Count == 0)
        {
            return Task.FromResult(new ConsensusResult
            {
                ConsensusReached = false,
                Confidence = 0
            });
        }

        var ordered = scoredResponses.OrderByDescending(r => r.Score).ToList();
        var winner = ordered[0];

        // Confidence is based on the gap between the best and second-best scores
        var confidence = ordered.Count > 1
            ? Math.Min(1.0, 0.5 + (winner.Score - ordered[1].Score))
            : 1.0;

        return Task.FromResult(new ConsensusResult
        {
            ConsensusReached = true,
            BestResponse = winner.Content,
            BestProvider = winner.ProviderName,
            BestScore = winner.Score,
            Confidence = confidence,
            AgreementCount = 1,
            TotalModels = scoredResponses.Count,
            AllScoredResponses = ordered,
            DissentingModels = ordered.Skip(1).Select(r => r.ProviderName).ToList()
        });
    }
}
