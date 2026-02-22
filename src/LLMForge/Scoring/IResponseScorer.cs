using LLMForge.Providers;

namespace LLMForge.Scoring;

/// <summary>
/// Defines a scorer that evaluates an LLM response and assigns a score.
/// </summary>
public interface IResponseScorer
{
    /// <summary>
    /// Gets the name of this scorer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Scores a single response, optionally in the context of all responses for comparison.
    /// </summary>
    /// <param name="response">The response to score.</param>
    /// <param name="allResponses">All responses for contextual scoring (e.g., consensus).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A score between 0.0 and 1.0.</returns>
    Task<double> ScoreAsync(
        LLMResponse response,
        IReadOnlyList<LLMResponse> allResponses,
        CancellationToken cancellationToken = default);
}
