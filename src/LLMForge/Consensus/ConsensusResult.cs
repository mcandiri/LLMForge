using LLMForge.Scoring;

namespace LLMForge.Consensus;

/// <summary>
/// Represents the result of a consensus evaluation across multiple LLM responses.
/// </summary>
public class ConsensusResult
{
    /// <summary>
    /// Gets or sets whether a consensus was reached.
    /// </summary>
    public bool ConsensusReached { get; set; }

    /// <summary>
    /// Gets or sets the winning response content.
    /// </summary>
    public string? BestResponse { get; set; }

    /// <summary>
    /// Gets or sets the provider that produced the best response.
    /// </summary>
    public string? BestProvider { get; set; }

    /// <summary>
    /// Gets or sets the score of the best response.
    /// </summary>
    public double BestScore { get; set; }

    /// <summary>
    /// Gets or sets the confidence level of the consensus (0.0 - 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the number of models that agreed.
    /// </summary>
    public int AgreementCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of models evaluated.
    /// </summary>
    public int TotalModels { get; set; }

    /// <summary>
    /// Gets or sets the names of models that dissented (disagreed with the consensus).
    /// </summary>
    public List<string> DissentingModels { get; set; } = new();

    /// <summary>
    /// Gets or sets all scored responses with their breakdowns.
    /// </summary>
    public List<ScoredResponse> AllScoredResponses { get; set; } = new();
}
