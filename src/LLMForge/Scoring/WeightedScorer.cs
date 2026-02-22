using LLMForge.Providers;

namespace LLMForge.Scoring;

/// <summary>
/// Combines multiple scorers with configurable weights to produce a composite score.
/// </summary>
public class WeightedScorer : IResponseScorer
{
    private readonly List<(IResponseScorer Scorer, double Weight)> _scorers = new();

    /// <inheritdoc />
    public string Name => "Weighted";

    /// <summary>
    /// Adds a scorer with its weight.
    /// </summary>
    /// <param name="scorer">The scorer.</param>
    /// <param name="weight">The weight (0.0 - 1.0). Weights are normalized at scoring time.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public WeightedScorer Add(IResponseScorer scorer, double weight)
    {
        ArgumentNullException.ThrowIfNull(scorer);
        if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be non-negative");

        _scorers.Add((scorer, weight));
        return this;
    }

    /// <summary>
    /// Gets all configured scorers and their weights.
    /// </summary>
    public IReadOnlyList<(IResponseScorer Scorer, double Weight)> Scorers => _scorers.AsReadOnly();

    /// <inheritdoc />
    public async Task<double> ScoreAsync(
        LLMResponse response,
        IReadOnlyList<LLMResponse> allResponses,
        CancellationToken cancellationToken = default)
    {
        if (_scorers.Count == 0) return 0.0;

        var totalWeight = _scorers.Sum(s => s.Weight);
        if (totalWeight <= 0) return 0.0;

        var weightedSum = 0.0;

        foreach (var (scorer, weight) in _scorers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var score = await scorer.ScoreAsync(response, allResponses, cancellationToken);
            weightedSum += score * weight;
        }

        return weightedSum / totalWeight;
    }

    /// <summary>
    /// Scores a response and returns the detailed breakdown.
    /// </summary>
    public async Task<ScoringResult> ScoreDetailedAsync(
        LLMResponse response,
        IReadOnlyList<LLMResponse> allResponses,
        CancellationToken cancellationToken = default)
    {
        var result = new ScoringResult
        {
            ProviderName = response.ProviderName
        };

        if (_scorers.Count == 0) return result;

        var totalWeight = _scorers.Sum(s => s.Weight);
        if (totalWeight <= 0) return result;

        var weightedSum = 0.0;

        foreach (var (scorer, weight) in _scorers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var score = await scorer.ScoreAsync(response, allResponses, cancellationToken);
            result.ScoreBreakdown[scorer.Name] = score;
            weightedSum += score * weight;
        }

        result.Score = weightedSum / totalWeight;
        return result;
    }
}
