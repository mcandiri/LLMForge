using LLMForge.Scoring;

namespace LLMForge.Consensus;

/// <summary>
/// Requires a minimum number of models to agree before accepting a response.
/// </summary>
public class QuorumStrategy : IConsensusStrategy
{
    private readonly int _requiredCount;
    private readonly double _similarityThreshold;

    /// <inheritdoc />
    public string Name => "Quorum";

    /// <summary>
    /// Initializes a new instance of the <see cref="QuorumStrategy"/>.
    /// </summary>
    /// <param name="requiredCount">Minimum number of agreeing models required.</param>
    /// <param name="similarityThreshold">The similarity threshold for agreement.</param>
    public QuorumStrategy(int requiredCount = 3, double similarityThreshold = 0.6)
    {
        if (requiredCount < 1)
            throw new ArgumentOutOfRangeException(nameof(requiredCount), "Quorum count must be at least 1");

        _requiredCount = requiredCount;
        _similarityThreshold = similarityThreshold;
    }

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

        // Find the response with the most "agreements"
        var bestCandidate = (ScoredResponse?)null;
        var bestAgreementCount = 0;
        var bestAgreeingProviders = new List<string>();

        foreach (var candidate in scoredResponses)
        {
            var agreeingProviders = new List<string> { candidate.ProviderName };

            foreach (var other in scoredResponses)
            {
                if (other.ProviderName == candidate.ProviderName) continue;

                var similarity = LLMForge.Scoring.ConsensusScorer.CalculateSimilarity(
                    candidate.Content, other.Content);

                if (similarity >= _similarityThreshold)
                {
                    agreeingProviders.Add(other.ProviderName);
                }
            }

            if (agreeingProviders.Count > bestAgreementCount)
            {
                bestAgreementCount = agreeingProviders.Count;
                bestCandidate = candidate;
                bestAgreeingProviders = agreeingProviders;
            }
        }

        var consensusReached = bestAgreementCount >= _requiredCount;
        var dissenters = scoredResponses
            .Where(r => !bestAgreeingProviders.Contains(r.ProviderName))
            .Select(r => r.ProviderName)
            .ToList();

        return Task.FromResult(new ConsensusResult
        {
            ConsensusReached = consensusReached,
            BestResponse = bestCandidate?.Content,
            BestProvider = bestCandidate?.ProviderName,
            BestScore = bestCandidate?.Score ?? 0,
            Confidence = (double)bestAgreementCount / scoredResponses.Count,
            AgreementCount = bestAgreementCount,
            TotalModels = scoredResponses.Count,
            AllScoredResponses = scoredResponses.ToList(),
            DissentingModels = dissenters
        });
    }
}
