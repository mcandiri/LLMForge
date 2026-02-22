using LLMForge.Providers;

namespace LLMForge.Scoring;

/// <summary>
/// Scores responses based on how similar they are to other responses (consensus alignment).
/// Uses a simple token-overlap similarity metric.
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

        var similarities = otherResponses
            .Select(other => CalculateSimilarity(response.Content, other.Content))
            .ToList();

        var averageSimilarity = similarities.Average();
        return Task.FromResult(averageSimilarity);
    }

    /// <summary>
    /// Calculates a simple token-overlap (Jaccard) similarity between two strings.
    /// </summary>
    internal static double CalculateSimilarity(string text1, string text2)
    {
        if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
            return 0.0;

        var tokens1 = Tokenize(text1);
        var tokens2 = Tokenize(text2);

        if (tokens1.Count == 0 || tokens2.Count == 0)
            return 0.0;

        var intersection = tokens1.Intersect(tokens2, StringComparer.OrdinalIgnoreCase).Count();
        var union = tokens1.Union(tokens2, StringComparer.OrdinalIgnoreCase).Count();

        return union == 0 ? 0.0 : (double)intersection / union;
    }

    private static HashSet<string> Tokenize(string text)
    {
        return text
            .Split(new[] { ' ', '\t', '\n', '\r', ',', '.', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '"', '\'' },
                StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => t.Length > 1)
            .ToHashSet();
    }
}
