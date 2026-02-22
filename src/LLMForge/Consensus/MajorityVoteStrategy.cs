using LLMForge.Scoring;

namespace LLMForge.Consensus;

/// <summary>
/// Selects the response that most models agree on, using token-overlap similarity.
/// </summary>
public class MajorityVoteStrategy : IConsensusStrategy
{
    private readonly double _similarityThreshold;

    /// <inheritdoc />
    public string Name => "MajorityVote";

    /// <summary>
    /// Initializes a new instance of the <see cref="MajorityVoteStrategy"/>.
    /// </summary>
    /// <param name="similarityThreshold">The similarity threshold (0.0 - 1.0) for considering two responses as "agreeing".</param>
    public MajorityVoteStrategy(double similarityThreshold = 0.6)
    {
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

        if (scoredResponses.Count == 1)
        {
            var only = scoredResponses[0];
            return Task.FromResult(new ConsensusResult
            {
                ConsensusReached = true,
                BestResponse = only.Content,
                BestProvider = only.ProviderName,
                BestScore = only.Score,
                Confidence = 1.0,
                AgreementCount = 1,
                TotalModels = 1,
                AllScoredResponses = scoredResponses.ToList()
            });
        }

        // Group responses by similarity
        var groups = GroupBySimilarity(scoredResponses);

        // Find the largest group
        var largestGroup = groups.OrderByDescending(g => g.Count).First();

        // Pick the highest-scored response from the largest group
        var winner = largestGroup.OrderByDescending(r => r.Score).First();
        var agreementCount = largestGroup.Count;

        var dissenters = scoredResponses
            .Where(r => !largestGroup.Contains(r))
            .Select(r => r.ProviderName)
            .ToList();

        var confidence = (double)agreementCount / scoredResponses.Count;

        return Task.FromResult(new ConsensusResult
        {
            ConsensusReached = agreementCount > scoredResponses.Count / 2.0,
            BestResponse = winner.Content,
            BestProvider = winner.ProviderName,
            BestScore = winner.Score,
            Confidence = confidence,
            AgreementCount = agreementCount,
            TotalModels = scoredResponses.Count,
            AllScoredResponses = scoredResponses.ToList(),
            DissentingModels = dissenters
        });
    }

    private List<List<ScoredResponse>> GroupBySimilarity(IReadOnlyList<ScoredResponse> responses)
    {
        var groups = new List<List<ScoredResponse>>();
        var assigned = new HashSet<int>();

        for (var i = 0; i < responses.Count; i++)
        {
            if (assigned.Contains(i)) continue;

            var group = new List<ScoredResponse> { responses[i] };
            assigned.Add(i);

            for (var j = i + 1; j < responses.Count; j++)
            {
                if (assigned.Contains(j)) continue;

                var similarity = LLMForge.Scoring.ConsensusScorer.CalculateSimilarity(
                    responses[i].Content, responses[j].Content);

                if (similarity >= _similarityThreshold)
                {
                    group.Add(responses[j]);
                    assigned.Add(j);
                }
            }

            groups.Add(group);
        }

        return groups;
    }
}
