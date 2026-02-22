using LLMForge.Scoring;

namespace LLMForge.Consensus;

/// <summary>
/// Defines a strategy for reaching consensus among multiple LLM responses.
/// </summary>
public interface IConsensusStrategy
{
    /// <summary>
    /// Gets the name of this consensus strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates the scored responses and selects a winner.
    /// </summary>
    /// <param name="scoredResponses">All scored responses to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The consensus result.</returns>
    Task<ConsensusResult> EvaluateAsync(
        IReadOnlyList<ScoredResponse> scoredResponses,
        CancellationToken cancellationToken = default);
}
