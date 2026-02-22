using LLMForge.Providers;

namespace LLMForge.Scoring;

/// <summary>
/// Scores responses based on how similar they are to other responses (consensus alignment).
/// Uses TF-IDF + cosine similarity for more accurate semantic comparison.
/// </summary>
public class ConsensusScorer : IResponseScorer
{
    /// <inheritdoc />
    public string Name => "Consensus";

    /// <inheritdoc />
    public Task<double> ScoreAsync(
        LLMResponse response,
        IReadOnlyList<LLMResponse> allResponses,
        CancellationToken cancellationToken = default)
    {
        var otherResponses = allResponses
            .Where(r => r.IsSuccess && r.ProviderName != response.ProviderName)
            .ToList();

        if (otherResponses.Count == 0)
            return Task.FromResult(1.0);

        // Build corpus from all successful responses for IDF computation
        var corpus = allResponses
            .Where(r => r.IsSuccess)
            .Select(r => r.Content)
            .ToList();

        var similarities = otherResponses
            .Select(other => CalculateSimilarity(response.Content, other.Content, corpus))
            .ToList();

        var averageSimilarity = similarities.Average();
        return Task.FromResult(averageSimilarity);
    }

    /// <summary>
    /// Calculates TF-IDF cosine similarity between two strings within a corpus.
    /// </summary>
    internal static double CalculateSimilarity(string text1, string text2, IReadOnlyList<string>? corpus = null)
    {
        if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
            return 0.0;

        return corpus != null
            ? TfIdfSimilarityCalculator.Calculate(text1, text2, corpus)
            : TfIdfSimilarityCalculator.Calculate(text1, text2);
    }
}
