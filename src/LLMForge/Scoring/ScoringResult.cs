namespace LLMForge.Scoring;

/// <summary>
/// Represents the scoring result for a single LLM response.
/// </summary>
public class ScoringResult
{
    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the final composite score (0.0 - 1.0).
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the individual score components.
    /// </summary>
    public Dictionary<string, double> ScoreBreakdown { get; set; } = new();
}

/// <summary>
/// Represents a scored response combining the response content with its score.
/// </summary>
public class ScoredResponse
{
    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the composite score.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the score breakdown.
    /// </summary>
    public Dictionary<string, double> ScoreBreakdown { get; set; } = new();

    /// <summary>
    /// Gets or sets the response time.
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the token count.
    /// </summary>
    public int TotalTokens { get; set; }
}
